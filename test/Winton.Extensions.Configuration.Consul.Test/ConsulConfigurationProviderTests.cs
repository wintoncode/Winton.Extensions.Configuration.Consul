using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Consul;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Winton.Extensions.Configuration.Consul.Parsers;

namespace Winton.Extensions.Configuration.Consul
{
    [TestFixture]
    [TestOf(nameof(ConsulConfigurationProvider))]
    internal class ConsulConfigurationProviderTests
    {
        private readonly Mock<IConfigurationParser> _configParserMock =
            new Mock<IConfigurationParser>(MockBehavior.Strict);

        private readonly Mock<IConsulConfigurationClient> _consulConfigClientMock =
            new Mock<IConsulConfigurationClient>(MockBehavior.Strict);

        private readonly Mock<IConsulConfigurationSource> _consulConfigSourceMock =
            new Mock<IConsulConfigurationSource>(MockBehavior.Strict);

        private ConsulConfigurationProvider _consulConfigProvider;

        internal sealed class Constructor : ConsulConfigurationProviderTests
        {
            [Test]
            public void ShouldThrowIfParserIsNull()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Parser).Returns((IConfigurationParser)null);

                Assert.That(
                    () =>
                        new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object),
                    Throws.TypeOf<ArgumentNullException>()
                          .And.Message.Contains(nameof(_consulConfigSourceMock.Object.Parser)));
            }
        }

        internal sealed class Load : ConsulConfigurationProviderTests
        {
            [SetUp]
            public void SetUp()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Parser).Returns(_configParserMock.Object);
                _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(false);

                _consulConfigProvider =
                    new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object);
            }

            [Test]
            public void ShouldCallSourceOnLoadExceptionActionWhenException()
            {
                var calledOnLoadException = false;

                _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(new Exception());
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(
                        context =>
                        {
                            context.Ignore = true;
                            calledOnLoadException = true;
                        });

                _consulConfigProvider.Load();

                Assert.That(calledOnLoadException, Is.True);
            }

            [Test]
            public void ShouldHaveEmptyDataIfConfigDoesNotExistAndIsOptional()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(true);
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.NotFound });

                _consulConfigProvider.Load();

                Assert.That(
                    _consulConfigProvider.GetChildKeys(Enumerable.Empty<string>(), string.Empty),
                    Is.EqualTo(Enumerable.Empty<string>()).AsCollection);
            }

            [Test]
            public void ShouldNotParseIfConfigBytesIsNull()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(true);
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(
                        new QueryResult<KVPair>
                        {
                            Response = new KVPair("Test") { Value = new List<byte>().ToArray() },
                            StatusCode = HttpStatusCode.OK
                        });

                _consulConfigProvider.Load();

                Assert.That(
                    () => _configParserMock.Verify(cp => cp.Parse(It.IsAny<MemoryStream>()), Times.Never),
                    Throws.Nothing);
            }

            [Test]
            public void ShouldNotThrowExceptionIfOnLoadExceptionIsSetToIgnore()
            {
                _consulConfigClientMock.Setup(ccc => ccc.GetConfig())
                                       .ThrowsAsync(new Exception("Failed to load from Consul agent"));
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(exceptionContext => { exceptionContext.Ignore = true; });

                Assert.That(() => _consulConfigProvider.Load(), Throws.Nothing);
            }

            [Test]
            [TestCase("Key", "Key")]
            [TestCase("KEY", "key", TestName = "ShouldParseLoadedConfigIntoCaseInsensitiveDictionary")]
            public void ShouldParseLoadedConfigIntoDictionary(string actualKey, string lookupKey)
            {
                _configParserMock.Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
                                 .Returns(new Dictionary<string, string> { { actualKey, "Value" } });
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(
                        new QueryResult<KVPair>
                        {
                            Response = new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() },
                            StatusCode = HttpStatusCode.OK
                        });

                _consulConfigProvider.Load();

                _consulConfigProvider.TryGet(lookupKey, out string actualValue);
                Assert.That(actualValue, Is.EqualTo("Value"));
            }

            [Test]
            public void ShouldSetExceptionInLoadExceptionContextWhenExceptionDuringLoad()
            {
                ConsulLoadExceptionContext actualExceptionContext = null;
                var expectedException = new Exception("Failed to load from Consul agent");

                _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(expectedException);
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(
                        context =>
                        {
                            context.Ignore = true;
                            actualExceptionContext = context;
                        });

                _consulConfigProvider.Load();

                Assert.That(actualExceptionContext.Exception, Is.SameAs(expectedException));
            }

            [Test]
            public void ShouldSetSourceInLoadExceptionContextWhenExceptionDuringLoad()
            {
                ConsulLoadExceptionContext actualExceptionContext = null;
                _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(new Exception());
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(
                        context =>
                        {
                            context.Ignore = true;
                            actualExceptionContext = context;
                        });

                _consulConfigProvider.Load();

                Assert.That(actualExceptionContext.Source, Is.SameAs(_consulConfigSourceMock.Object));
            }

            [Test]
            public void ShouldThrowExceptionIfOnLoadExceptionDoesNotSetIgnoreWhenExceptionDuringLoad()
            {
                _consulConfigClientMock.Setup(ccc => ccc.GetConfig())
                                       .ThrowsAsync(new Exception("Error"));
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(exceptionContext => { exceptionContext.Ignore = false; });

                Assert.That(
                    () => _consulConfigProvider.Load(),
                    Throws.TypeOf<Exception>().And.Message.Matches("Error"));
            }

            [Test]
            public void ShouldThrowIfConfigDoesNotExistAndIsNotOptonalWhenLoad()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(false);
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.NotFound });
                _consulConfigSourceMock.Setup(ccs => ccs.Key).Returns("Test");
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(context => context.Ignore = false);

                Assert.That(
                    () => _consulConfigProvider.Load(),
                    Throws.TypeOf<Exception>().And.Message.Matches(
                        "The configuration for key Test was not found and is not optional."));
            }
        }

        internal sealed class Reload : ConsulConfigurationProviderTests
        {
            private ConfigurationReloadToken _firstChangeToken;

            [SetUp]
            public void SetUp()
            {
                _firstChangeToken = new ConfigurationReloadToken();
                _consulConfigClientMock
                    .SetupSequence(ccc => ccc.Watch(It.IsAny<Action<ConsulWatchExceptionContext>>()))
                    .Returns(_firstChangeToken)
                    .Returns(new ConfigurationReloadToken());
                _consulConfigSourceMock.Setup(ccs => ccs.OnWatchException).Returns(context => { });
                _consulConfigSourceMock.Setup(ccs => ccs.Parser).Returns(_configParserMock.Object);
                _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(true);

                _consulConfigProvider = new ConsulConfigurationProvider(
                    _consulConfigSourceMock.Object,
                    _consulConfigClientMock.Object);
            }

            [Test]
            public void ShouldNotOverwriteNonOptionalConfigIfDoesNotExist()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(false);
                _consulConfigClientMock
                    .SetupSequence(ccc => ccc.GetConfig())
                    .ReturnsAsync(
                        new QueryResult<KVPair>
                        {
                            Response = new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() },
                            StatusCode = HttpStatusCode.OK
                        })
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.NotFound });

                _configParserMock.Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
                                 .Returns(new Dictionary<string, string> { { "Key", "Test" } });

                _consulConfigProvider.Load();

                _firstChangeToken.OnReload();

                Assert.That(_consulConfigProvider.TryGet("Key", out string _), Is.True);
            }

            [Test]
            [TestCase(false, TestName = "ShouldNotThrowIfDoesNotExistOnReloadWhenConfigOptional")]
            [TestCase(false, TestName = "ShouldNotThrowIfDoesNotExistOnReloadWhenConfigIsNotOptional")]
            public void ShouldNotThrowIfDoesNotExist(bool optional)
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(optional);
                _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(true);
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.NotFound });

                Assert.That(_firstChangeToken.OnReload, Throws.Nothing);
            }

            [Test]
            public void ShouldReloadConfigIfReloadOnChangeAndDataInConsulHasChanged()
            {
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.OK });

                _firstChangeToken.OnReload();

                Assert.That(() => _consulConfigClientMock.Verify(ccc => ccc.GetConfig()), Throws.Nothing);
            }

            [Test]
            public void ShouldWatchForChangesIfSourceReloadOnChangesIsTrue()
            {
                Assert.That(
                    () =>
                        _consulConfigClientMock.Verify(
                            ccs => ccs.Watch(_consulConfigSourceMock.Object.OnWatchException)),
                    Throws.Nothing);
            }
        }
    }
}