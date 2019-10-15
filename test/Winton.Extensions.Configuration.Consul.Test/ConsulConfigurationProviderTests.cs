using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using FluentAssertions;
using Moq;
using Winton.Extensions.Configuration.Consul.Parsers;
using Xunit;
using Range = Moq.Range;

namespace Winton.Extensions.Configuration.Consul
{
    public class ConsulConfigurationProviderTests
    {
        private readonly Mock<IKVEndpoint> _kvEndpoint;
        private readonly Mock<IConfigurationParser> _parser;
        private readonly ConsulConfigurationProvider _provider;
        private readonly IConsulConfigurationSource _source;

        public ConsulConfigurationProviderTests()
        {
            _kvEndpoint = new Mock<IKVEndpoint>(MockBehavior.Strict);
            var consulClient = new Mock<IConsulClient>(MockBehavior.Strict);
            consulClient
                .Setup(cc => cc.KV)
                .Returns(_kvEndpoint.Object);
            consulClient
                .Setup(cc => cc.Dispose());
            var consulClientFactory = new Mock<IConsulClientFactory>(MockBehavior.Strict);
            consulClientFactory
                .Setup(ccf => ccf.Create())
                .Returns(consulClient.Object);
            _parser = new Mock<IConfigurationParser>(MockBehavior.Strict);
            _source = new ConsulConfigurationSource("Test")
            {
                Parser = _parser.Object
            };
            _provider = new ConsulConfigurationProvider(
                _source,
                consulClientFactory.Object);
        }

        public sealed class Constructor : ConsulConfigurationProviderTests
        {
            [Fact]
            public void ShouldThrowIfParserIsNull()
            {
                var source = new ConsulConfigurationSource("Test")
                {
                    Parser = null!
                };

                // ReSharper disable once ObjectCreationAsStatement
                Action constructing =
                    () =>
                        new ConsulConfigurationProvider(source, new Mock<IConsulClientFactory>().Object);

                constructing
                    .Should()
                    .Throw<ArgumentNullException>()
                    .And.Message.Should().Contain(nameof(IConsulConfigurationSource.Parser));
            }
        }

        public sealed class Dispose : ConsulConfigurationProviderTests
        {
            [Fact]
            private async Task ShouldCancelPollingTaskWhenReloading()
            {
                var expectedKvCalls = 0;
                var pollingCancelled = new TaskCompletionSource<CancellationToken>();
                CancellationToken cancellationToken = default;
                _source.ReloadOnChange = true;
                _source.Optional = true;
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .Callback<string, QueryOptions, CancellationToken>(
                        (_, __, token) =>
                        {
                            if (pollingCancelled.Task.IsCompleted)
                            {
                                return;
                            }

                            expectedKvCalls++;
                            if (cancellationToken == default)
                            {
                                cancellationToken = token;
                            }
                        })
                    .Returns(
                        pollingCancelled.Task.IsCompleted
                            ? Task.FromCanceled<QueryResult<KVPair[]>>(pollingCancelled.Task.Result)
                            : Task.FromResult(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.OK }));

                _provider.Load();
                cancellationToken.Register(() => pollingCancelled.SetResult(cancellationToken));

                _provider.Dispose();

                await pollingCancelled.Task;

                // It's possible that one additional call to KV List endpoint is made depending on when the loop is interrupted.
                _kvEndpoint.Verify(
                    kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()),
                    Times.Between(expectedKvCalls, expectedKvCalls + 1, Range.Inclusive));
            }
        }

        public sealed class DoNotReloadOnChange : ConsulConfigurationProviderTests
        {
            public DoNotReloadOnChange()
            {
                _source.ReloadOnChange = false;
            }

            [Fact]
            private void ShouldCallLoadExceptionWhenConsulReturnsBadRequest()
            {
                var calledOnLoadException = false;
                _source.OnLoadException = ctx =>
                {
                    ctx.Ignore = true;
                    calledOnLoadException = true;
                };
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            StatusCode = HttpStatusCode.BadRequest
                        });

                _provider.Load();

                calledOnLoadException.Should().BeTrue();
            }

            [Fact]
            private void ShouldCallOnLoadExceptionActionWhenLoadingThrows()
            {
                var calledOnLoadException = false;

                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception());
                _source.OnLoadException = ctx =>
                {
                    ctx.Ignore = true;
                    calledOnLoadException = true;
                };

                _provider.Load();

                calledOnLoadException.Should().BeTrue();
            }

            [Fact]
            private void ShouldHaveEmptyDataWhenConfigDoesNotExistAndIsOptional()
            {
                _source.Optional = true;
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.NotFound });

                _provider.Load();

                _provider.GetChildKeys(Enumerable.Empty<string>(), string.Empty).Should().BeEmpty();
            }

            [Fact]
            private void ShouldNotMakeABlockingCall()
            {
                var allQueryOptions = new List<QueryOptions>();
                _parser
                    .Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
                    .Returns(new Dictionary<string, string> { { "Key", "Value" } });
                _kvEndpoint
                    .Setup(
                        kv =>
                            kv.List(
                                "Test",
                                It.IsAny<QueryOptions>(),
                                It.IsAny<CancellationToken>()))
                    .Callback<string, QueryOptions, CancellationToken>(
                        (_, options, __) => allQueryOptions.Add(options))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            LastIndex = 1234,
                            Response = new[]
                            {
                                new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        });

                _provider.Load();
                _provider.Load();

                allQueryOptions
                    .Should()
                    .NotBeEmpty()
                    .And
                    .AllBeEquivalentTo(new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime });
            }

            [Fact]
            private void ShouldNotThrowExceptionIfOnLoadExceptionIsSetToIgnore()
            {
                _source.OnLoadException = ctx => ctx.Ignore = true;
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Failed to load from Consul agent"));

                Action loading = () => _provider.Load();

                loading.Should().NotThrow();
            }

            [Fact]
            private void ShouldReloadWhenNotPolling()
            {
                _source.Optional = true;
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.NotFound });

                _provider.Load();
                _provider.Load();

                _kvEndpoint.Verify(
                    kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()),
                    Times.Exactly(2));
            }

            [Fact]
            private void ShouldSetData()
            {
                _parser
                    .Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
                    .Returns(new Dictionary<string, string> { { "Key", "Value" } });
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            Response = new[]
                            {
                                new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        });

                _provider.Load();

                _provider.TryGet("Key", out var value);
                value.Should().Be("Value");
            }

            [Fact]
            private void ShouldSetLoadExceptionContextWhenExceptionDuringLoad()
            {
                ConsulLoadExceptionContext? exceptionContext = null;
                var exception = new Exception("Failed to load from Consul agent");

                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);
                _source.OnLoadException = ctx =>
                {
                    ctx.Ignore = true;
                    exceptionContext = ctx;
                };

                _provider.Load();

                exceptionContext
                    .Should()
                    .BeEquivalentTo(new ConsulLoadExceptionContext(_source, exception) { Ignore = true });
            }

            [Fact]
            private void ShouldThrowExceptionIfNotIgnoredByClient()
            {
                _source.OnLoadException = ctx => ctx.Ignore = false;
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Error"));

                var loading = _provider.Invoking(p => p.Load());

                loading.Should().Throw<Exception>().WithMessage("Error");
            }

            [Fact]
            private void ShouldThrowWhenConfigDoesNotExistAndIsNotOptional()
            {
                _source.Optional = false;
                _source.OnLoadException = ctx => ctx.Ignore = false;
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.NotFound });

                var loading = _provider.Invoking(p => p.Load());
                loading
                    .Should()
                    .Throw<Exception>()
                    .WithMessage("The configuration for key Test was not found and is not optional.");
            }
        }

        public sealed class ReloadOnChange : ConsulConfigurationProviderTests
        {
            public ReloadOnChange()
            {
                _source.ReloadOnChange = true;
            }

            [Fact]
            private async Task ShouldCallOnWatchExceptionWithCountOfConsecutiveFailures()
            {
                var exceptionContexts = new List<ConsulWatchExceptionContext>();
                _source.Optional = true;
                _source.OnWatchException = ctx =>
                {
                    exceptionContexts.Add(ctx);
                    return TimeSpan.Zero;
                };

                var pollingCompleted = new TaskCompletionSource<bool>();

                var exception1 = new Exception("Error during watch 1.");
                var exception2 = new Exception("Error during watch 2.");
                _kvEndpoint
                    .SetupSequence(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK })
                    .ThrowsAsync(exception1)
                    .ThrowsAsync(exception2)
                    .Returns(
                        () =>
                        {
                            pollingCompleted.SetResult(true);
                            return new TaskCompletionSource<QueryResult<KVPair[]>>().Task;
                        });

                _provider.Load();

                await pollingCompleted.Task;

                exceptionContexts
                    .Should()
                    .BeEquivalentTo(
                        new List<ConsulWatchExceptionContext>
                        {
                            new ConsulWatchExceptionContext(exception1, 1, _source),
                            new ConsulWatchExceptionContext(exception2, 2, _source)
                        });
            }

            [Fact]
            private async Task ShouldNotOverwriteNonOptionalConfigIfDoesNotExist()
            {
                var pollingCompleted = new TaskCompletionSource<bool>();
                _source.Optional = false;
                _kvEndpoint
                    .SetupSequence(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            Response = new[]
                            {
                                new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        })
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.NotFound })
                    .Returns(
                        () =>
                        {
                            pollingCompleted.SetResult(true);
                            return new TaskCompletionSource<QueryResult<KVPair[]>>().Task;
                        });
                _parser
                    .Setup(cp => cp.Parse(It.IsAny<MemoryStream>()))
                    .Returns(new Dictionary<string, string> { { "Key", "Test" } });

                _provider.Load();

                await pollingCompleted.Task;

                _provider.TryGet("Key", out _).Should().BeTrue();
            }

            [Fact]
            private void ShouldNotReloadWhenPolling()
            {
                _source.Optional = true;
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK });

                _provider.Load();
                _provider.Load();

                _kvEndpoint.Verify(
                    kv =>
                        kv.List(
                            "Test",
                            It.Is<QueryOptions>(options => options.WaitIndex == 0),
                            It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Fact]
            private async Task ShouldReloadConfigWhenDataInConsulHasChanged()
            {
                var reload = new TaskCompletionSource<bool>();
                _provider
                    .GetReloadToken()
                    .RegisterChangeCallback(_ => reload.TrySetResult(true), new object());
                _kvEndpoint
                    .SetupSequence(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            LastIndex = 12,
                            Response = new[]
                            {
                                new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        })
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            LastIndex = 13,
                            Response = new[]
                            {
                                new KVPair("Test") { Value = new List<byte> { 1 }.ToArray() }
                            },
                            StatusCode = HttpStatusCode.OK
                        })
                    .Returns(new TaskCompletionSource<QueryResult<KVPair[]>>().Task);
                _parser
                    .SetupSequence(p => p.Parse(It.IsAny<Stream>()))
                    .Returns(new Dictionary<string, string> { { "Key", "Test" } })
                    .Returns(new Dictionary<string, string> { { "Key", "Test2" } });

                _provider.Load();

                await reload.Task;

                _provider.TryGet("Key", out var value);
                value.Should().Be("Test2");
            }

            [Fact]
            private async Task ShouldResetConsecutiveFailureCountAfterASuccessfulPoll()
            {
                var exceptionContexts = new List<ConsulWatchExceptionContext>();
                _source.Optional = true;
                _source.OnWatchException = ctx =>
                {
                    exceptionContexts.Add(ctx);
                    return TimeSpan.Zero;
                };

                var pollingCompleted = new TaskCompletionSource<bool>();

                var exception1 = new Exception("Error during watch 1.");
                var exception2 = new Exception("Error during watch 2.");
                _kvEndpoint
                    .SetupSequence(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK })
                    .ThrowsAsync(exception1)
                    .ReturnsAsync(new QueryResult<KVPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK })
                    .ThrowsAsync(exception2)
                    .Returns(
                        () =>
                        {
                            pollingCompleted.SetResult(true);
                            return new TaskCompletionSource<QueryResult<KVPair[]>>().Task;
                        });

                _provider.Load();

                await pollingCompleted.Task;

                exceptionContexts
                    .Should()
                    .BeEquivalentTo(
                        new List<ConsulWatchExceptionContext>
                        {
                            new ConsulWatchExceptionContext(exception1, 1, _source),
                            new ConsulWatchExceptionContext(exception2, 1, _source)
                        });
            }

            [Fact]
            private async Task ShouldResetLastIndexWhenItGoesBackwards()
            {
                _source.Optional = true;
                var queryOptions = new List<QueryOptions>();
                var pollingCompleted = new TaskCompletionSource<bool>();

                var results = new Queue<QueryResult<KVPair[]>>(
                    new List<QueryResult<KVPair[]>>
                    {
                        new QueryResult<KVPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK },
                        new QueryResult<KVPair[]> { LastIndex = 12, StatusCode = HttpStatusCode.OK }
                    });
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .Returns<string, QueryOptions, CancellationToken>(
                        (_, options, __) =>
                        {
                            queryOptions.Add(options);
                            if (results.TryDequeue(out var result))
                            {
                                return Task.FromResult(result);
                            }

                            pollingCompleted.SetResult(true);
                            return new TaskCompletionSource<QueryResult<KVPair[]>>().Task;
                        });

                _provider.Load();

                await pollingCompleted.Task;

                queryOptions
                    .Should()
                    .BeEquivalentTo(
                        new List<QueryOptions>
                        {
                            new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime },
                            new QueryOptions { WaitIndex = 13, WaitTime = _source.PollWaitTime },
                            new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime }
                        });
            }

            [Fact]
            private async Task ShouldSetLastIndexToOneWhenConsulReturnsIndexNotGreaterThanZero()
            {
                _source.Optional = true;
                var queryOptions = new List<QueryOptions>();
                var pollingCompleted = new TaskCompletionSource<bool>();

                var results = new Queue<QueryResult<KVPair[]>>(
                    new List<QueryResult<KVPair[]>>
                    {
                        new QueryResult<KVPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK },
                        new QueryResult<KVPair[]> { LastIndex = 0, StatusCode = HttpStatusCode.OK },
                        new QueryResult<KVPair[]> { LastIndex = 0, StatusCode = HttpStatusCode.OK }
                    });
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .Returns<string, QueryOptions, CancellationToken>(
                        (_, options, __) =>
                        {
                            queryOptions.Add(options);
                            if (results.TryDequeue(out var result))
                            {
                                return Task.FromResult(result);
                            }

                            pollingCompleted.SetResult(true);
                            return new TaskCompletionSource<QueryResult<KVPair[]>>().Task;
                        });

                _provider.Load();

                await pollingCompleted.Task;

                queryOptions
                    .Should()
                    .BeEquivalentTo(
                        new List<QueryOptions>
                        {
                            new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime },
                            new QueryOptions { WaitIndex = 13, WaitTime = _source.PollWaitTime },
                            new QueryOptions { WaitIndex = 1, WaitTime = _source.PollWaitTime },
                            new QueryOptions { WaitIndex = 1, WaitTime = _source.PollWaitTime }
                        });
            }

            [Fact]
            private async Task ShouldWaitForChangesAfterInitialLoad()
            {
                _source.Optional = true;
                var queryOptions = new List<QueryOptions>();
                var pollingCompleted = new TaskCompletionSource<bool>();

                var results = new Queue<QueryResult<KVPair[]>>(
                    new List<QueryResult<KVPair[]>>
                    {
                        new QueryResult<KVPair[]> { LastIndex = 13, StatusCode = HttpStatusCode.OK }
                    });
                _kvEndpoint
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .Returns<string, QueryOptions, CancellationToken>(
                        (_, options, __) =>
                        {
                            queryOptions.Add(options);
                            if (results.TryDequeue(out var result))
                            {
                                return Task.FromResult(result);
                            }

                            pollingCompleted.SetResult(true);
                            return new TaskCompletionSource<QueryResult<KVPair[]>>().Task;
                        });

                _provider.Load();

                await pollingCompleted.Task;

                queryOptions
                    .Should()
                    .BeEquivalentTo(
                        new List<QueryOptions>
                        {
                            new QueryOptions { WaitIndex = 0, WaitTime = _source.PollWaitTime },
                            new QueryOptions { WaitIndex = 13, WaitTime = _source.PollWaitTime }
                        });
            }

            [Fact]
            private async Task ShouldWatchForChangesIfSourceReloadOnChangesIsTrue()
            {
                var pollingCompleted = new TaskCompletionSource<bool>();
                _source.Optional = true;
                _kvEndpoint
                    .SetupSequence(kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryResult<KVPair[]> { StatusCode = HttpStatusCode.OK })
                    .Returns(
                        () =>
                        {
                            pollingCompleted.SetResult(true);
                            return new TaskCompletionSource<QueryResult<KVPair[]>>().Task;
                        });

                _provider.Load();

                await pollingCompleted.Task;

                _kvEndpoint.Verify(
                    kv => kv.List("Test", It.IsAny<QueryOptions>(), It.IsAny<CancellationToken>()),
                    Times.Exactly(2));
            }
        }
    }
}