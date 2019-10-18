using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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

        private ConsulConfigurationProvider _consulConfigProvider;

        public sealed class Constructor : ConsulConfigurationProviderTests
        {
            [Fact]
            public void ShouldThrowIfParserIsNull()
            {
                var source = new ConsulConfigurationSource("Test")
                {
                    Parser = null
                };

                // ReSharper disable once ObjectCreationAsStatement
                Action constructing =
                    () =>
                        new ConsulConfigurationProvider(source, _consulConfigClientMock.Object);

                constructing.Should().Throw<ArgumentNullException>()
                            .And.Message.Should().Contain(nameof(IConsulConfigurationSource.Parser));
            }
        }

        public sealed class Load : ConsulConfigurationProviderTests
        {
            private readonly ConsulConfigurationSource _source;

            public Load()
            {
                _source = new ConsulConfigurationSource("path/test")
                {
                    Parser = _configParserMock.Object,
                    ReloadOnChange = false
                };

                _consulConfigProvider = new ConsulConfigurationProvider(_source, _consulConfigClientMock.Object);
            }

            [Fact]
            private void ShouldCallSourceOnLoadExceptionActionWhenException()
            {
                var calledOnLoadException = false;

                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception());
                _source.OnLoadException = context =>
                {
                    context.Ignore = true;
                    calledOnLoadException = true;
                };

                _consulConfigProvider.Load();

                calledOnLoadException.Should().BeTrue();
            }

            [Fact]
            private void ShouldHaveEmptyDataIfConfigDoesNotExistAndIsOptional()
            {
                _source.Optional = true;
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.NotFound });

                _consulConfigProvider.Load();

                _consulConfigProvider.GetChildKeys(Enumerable.Empty<string>(), string.Empty).Should().BeEmpty();
            }

            [Fact]
            private void ShouldNotParseIfConfigBytesIsNull()
            {
                _source.Optional = true;
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            Response = new[]
                            {
                                new KVPair("path/test") { Value = new List<byte>().ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        });

                _consulConfigProvider.Load();

                Action verifying = () => _configParserMock.Verify(cp => cp.Parse(It.IsAny<MemoryStream>()), Times.Never);
                verifying.Should().NotThrow();
            }

            [Fact]
            private void ShouldNotThrowExceptionIfOnLoadExceptionIsSetToIgnore()
            {
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Failed to load from Consul agent"));
                _source.OnLoadException = exceptionContext => { exceptionContext.Ignore = true; };

                Action loading = () => _consulConfigProvider.Load();
                loading.Should().NotThrow();
            }

            [Theory]
            [InlineData("Key")]
            [InlineData("KEY")]
            [InlineData("key")]
            [InlineData("KeY")]
            private void ShouldParseLoadedConfigIntoCaseInsensitiveDictionary(string lookupKey)
            {
                _configParserMock
                    .Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
                    .Returns(new Dictionary<string, string> { { "kEy", "Value" } });
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            Response = new[]
                            {
                                new KVPair("path/test") { Value = new List<byte> { 1 }.ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        });

                _consulConfigProvider.Load();

                _consulConfigProvider.TryGet(lookupKey, out string value);
                value.Should().Be("Value");
            }

            [Fact]
            private void ShouldRemoveCustomKeySectionIfSpecified()
            {
                _source.KeyToRemove = "path";
                _configParserMock
                    .Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
                    .Returns(new Dictionary<string, string> { { "Key", "Value" } });
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            Response = new[]
                            {
                                new KVPair("path/test") { Value = new List<byte> { 1 }.ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        });

                _consulConfigProvider.Load();

                _consulConfigProvider.TryGet("test:Key", out string value);
                value.Should().Be("Value");
            }

            [Fact]
            private void ShouldSetExceptionInLoadExceptionContextWhenExceptionDuringLoad()
            {
                ConsulLoadExceptionContext exceptionContext = null;
                var expectedException = new Exception("Failed to load from Consul agent");

                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(expectedException);
                _source.OnLoadException = context =>
                {
                    context.Ignore = true;
                    exceptionContext = context;
                };

                _consulConfigProvider.Load();

                exceptionContext.Exception.Should().BeSameAs(expectedException);
            }

            [Fact]
            private void ShouldSetSourceInLoadExceptionContextWhenExceptionDuringLoad()
            {
                ConsulLoadExceptionContext exceptionContext = null;
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception());
                _source.OnLoadException = context =>
                {
                    context.Ignore = true;
                    exceptionContext = context;
                };

                _consulConfigProvider.Load();

                exceptionContext.Source.Should().BeSameAs(_source);
            }

            [Fact]
            private void ShouldThrowExceptionIfOnLoadExceptionDoesNotSetIgnoreWhenExceptionDuringLoad()
            {
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Error"));
                _source.OnLoadException = exceptionContext => { exceptionContext.Ignore = false; };

                Action loading = _consulConfigProvider.Invoking(ccp => ccp.Load());
                loading.Should().Throw<Exception>().WithMessage("Error");
            }

            [Fact]
            private void ShouldThrowIfConfigDoesNotExistAndIsNotOptonalWhenLoad()
            {
                _source.Optional = false;
                _source.OnLoadException = context => context.Ignore = false;
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("path/test", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.NotFound });

                Action loading = _consulConfigProvider.Invoking(ccp => ccp.Load());
                loading.Should().Throw<Exception>()
                       .WithMessage("The configuration for key path/test was not found and is not optional.");
            }
        }

        public sealed class Reload : ConsulConfigurationProviderTests
        {
            private readonly ConfigurationReloadToken _firstChangeToken;
            private readonly IConsulConfigurationSource _source;

            public Reload()
            {
                _source = new ConsulConfigurationSource("Test")
                {
                    Parser = _configParserMock.Object,
                    ReloadOnChange = true
                };
                _firstChangeToken = new ConfigurationReloadToken();
                _consulConfigClientMock
                    .SetupSequence(
                        ccc =>
                            ccc.Watch(
                                "Test",
                                It.IsAny<Func<ConsulWatchExceptionContext, TimeSpan>>(),
                                It.IsAny<CancellationToken>()))
                    .Returns(_firstChangeToken)
                    .Returns(new ConfigurationReloadToken());

                _consulConfigProvider = new ConsulConfigurationProvider(
                    _source,
                    _consulConfigClientMock.Object);
            }

            [Fact]
            private void ShouldNotOverwriteNonOptionalConfigIfDoesNotExist()
            {
                _source.Optional = false;
                _consulConfigClientMock
                    .SetupSequence(ccc => ccc.GetConfig("Test", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            Response = new[]
                            {
                                new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        })
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.NotFound });
                _configParserMock
                    .Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
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
                _source.Optional = optional;
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("Test", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.NotFound });

                Action reloading = _firstChangeToken.Invoking(ct => ct.OnReload());
                reloading.Should().NotThrow();
            }

            [Fact]
            private void ShouldReloadConfigIfReloadOnChangeAndDataInConsulHasChanged()
            {
                _consulConfigClientMock
                    .Setup(ccc => ccc.GetConfig("Test", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.OK });

                _firstChangeToken.OnReload();

                Action verifying = () => _consulConfigClientMock
                    .Verify(ccc => ccc.GetConfig("Test", It.IsAny<CancellationToken>()), Times.Once);
                verifying.Should().NotThrow();
            }

            [Fact]
            private void ShouldWatchForChangesIfSourceReloadOnChangesIsTrue()
            {
                Action verifying =
                    () =>
                        _consulConfigClientMock.Verify(
                            ccs => ccs.Watch("Test", _source.OnWatchException, It.IsAny<CancellationToken>()),
                            Times.Once);
                verifying.Should().NotThrow();
            }
        }
    }
}