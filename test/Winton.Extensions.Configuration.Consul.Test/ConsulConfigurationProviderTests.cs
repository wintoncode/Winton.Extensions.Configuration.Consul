using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Consul;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Winton.Extensions.Configuration.Consul.Parsers;
using Xunit;

namespace Winton.Extensions.Configuration.Consul
{
    public class ConsulConfigurationProviderTests
    {
        private readonly Mock<IConfigurationParser> _configParserMock =
            new Mock<IConfigurationParser>(MockBehavior.Strict);

        private readonly Mock<IConsulConfigurationClient> _consulConfigClientMock =
            new Mock<IConsulConfigurationClient>(MockBehavior.Strict);

        private readonly Mock<IConsulConfigurationSource> _consulConfigSourceMock =
            new Mock<IConsulConfigurationSource>(MockBehavior.Strict);

        private ConsulConfigurationProvider _consulConfigProvider;

        public sealed class Constructor : ConsulConfigurationProviderTests
        {
            [Fact]
            public void ShouldThrowIfParserIsNull()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Parser).Returns((IConfigurationParser)null);

                // ReSharper disable once ObjectCreationAsStatement
                Action constructing =
                    () =>
                        new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object);

                constructing.Should().Throw<ArgumentNullException>()
                            .And.Message.Should().Contain(nameof(_consulConfigSourceMock.Object.Parser));
            }
        }

        public sealed class Load : ConsulConfigurationProviderTests
        {
            public Load()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Parser).Returns(_configParserMock.Object);
                _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(false);

                _consulConfigProvider = new ConsulConfigurationProvider(
                    _consulConfigSourceMock.Object,
                    _consulConfigClientMock.Object);
            }

            [Fact]
            private void ShouldCallSourceOnLoadExceptionActionWhenException()
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

                calledOnLoadException.Should().BeTrue();
            }

            [Fact]
            private void ShouldHaveEmptyDataIfConfigDoesNotExistAndIsOptional()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(true);
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.NotFound });

                _consulConfigProvider.Load();

                _consulConfigProvider.GetChildKeys(Enumerable.Empty<string>(), string.Empty).Should().BeEmpty();
            }

            [Fact]
            private void ShouldNotParseIfConfigBytesIsNull()
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

                Action verifying = () => _configParserMock.Verify(cp => cp.Parse(It.IsAny<MemoryStream>()), Times.Never);
                verifying.Should().NotThrow();
            }

            [Fact]
            private void ShouldNotThrowExceptionIfOnLoadExceptionIsSetToIgnore()
            {
                _consulConfigClientMock.Setup(ccc => ccc.GetConfig())
                                       .ThrowsAsync(new Exception("Failed to load from Consul agent"));
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(exceptionContext => { exceptionContext.Ignore = true; });

                Action loading = () => _consulConfigProvider.Load();
                loading.Should().NotThrow();
            }

            [Theory]
            [InlineData("Key", "Key")]
            [InlineData("KEY", "key")]
            private void ShouldParseLoadedConfigIntoDictionary(string key, string lookupKey)
            {
                _configParserMock.Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
                                 .Returns(new Dictionary<string, string> { { key, "Value" } });
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(
                        new QueryResult<KVPair>
                        {
                            Response = new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() },
                            StatusCode = HttpStatusCode.OK
                        });

                _consulConfigProvider.Load();

                _consulConfigProvider.TryGet(lookupKey, out string value);
                value.Should().Be("Value");
            }

            [Fact]
            private void ShouldSetExceptionInLoadExceptionContextWhenExceptionDuringLoad()
            {
                ConsulLoadExceptionContext exceptionContext = null;
                var expectedException = new Exception("Failed to load from Consul agent");

                _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(expectedException);
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(
                        context =>
                        {
                            context.Ignore = true;
                            exceptionContext = context;
                        });

                _consulConfigProvider.Load();

                exceptionContext.Exception.Should().BeSameAs(expectedException);
            }

            [Fact]
            private void ShouldSetSourceInLoadExceptionContextWhenExceptionDuringLoad()
            {
                ConsulLoadExceptionContext exceptionContext = null;
                _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(new Exception());
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(
                        context =>
                        {
                            context.Ignore = true;
                            exceptionContext = context;
                        });

                _consulConfigProvider.Load();

                exceptionContext.Source.Should().BeSameAs(_consulConfigSourceMock.Object);
            }

            [Fact]
            private void ShouldThrowExceptionIfOnLoadExceptionDoesNotSetIgnoreWhenExceptionDuringLoad()
            {
                _consulConfigClientMock.Setup(ccc => ccc.GetConfig())
                                       .ThrowsAsync(new Exception("Error"));
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(exceptionContext => { exceptionContext.Ignore = false; });

                Action loading = _consulConfigProvider.Invoking(ccp => ccp.Load());
                loading.Should().Throw<Exception>().WithMessage("Error");
            }

            [Fact]
            private void ShouldThrowIfConfigDoesNotExistAndIsNotOptonalWhenLoad()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(false);
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.NotFound });
                _consulConfigSourceMock.Setup(ccs => ccs.Key).Returns("Test");
                _consulConfigSourceMock
                    .Setup(ccs => ccs.OnLoadException)
                    .Returns(context => context.Ignore = false);

                Action loading = _consulConfigProvider.Invoking(ccp => ccp.Load());
                loading.Should().Throw<Exception>()
                       .WithMessage("The configuration for key Test was not found and is not optional.");
            }
        }

        public sealed class Reload : ConsulConfigurationProviderTests
        {
            private readonly ConfigurationReloadToken _firstChangeToken;

            public Reload()
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

            [Fact]
            private void ShouldNotOverwriteNonOptionalConfigIfDoesNotExist()
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

                _consulConfigProvider.TryGet("Key", out string _).Should().BeTrue();
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            private void ShouldNotThrowIfDoesNotExist(bool optional)
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(optional);
                _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(true);
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.NotFound });

                Action reloading = _firstChangeToken.Invoking(ct => ct.OnReload());
                reloading.Should().NotThrow();
            }

            [Fact]
            private void ShouldReloadConfigIfReloadOnChangeAndDataInConsulHasChanged()
            {
                _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(true);
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig())
                    .ReturnsAsync(new QueryResult<KVPair> { StatusCode = HttpStatusCode.OK });

                _firstChangeToken.OnReload();

                Action verifying = () => _consulConfigClientMock.Verify(ccc => ccc.GetConfig(), Times.Once);
                verifying.Should().NotThrow();
            }

            [Fact]
            private void ShouldWatchForChangesIfSourceReloadOnChangesIsTrue()
            {
                Action verifying =
                    () =>
                        _consulConfigClientMock.Verify(
                            ccs => ccs.Watch(_consulConfigSourceMock.Object.OnWatchException),
                            Times.Once);
                verifying.Should().NotThrow();
            }
        }
    }
}