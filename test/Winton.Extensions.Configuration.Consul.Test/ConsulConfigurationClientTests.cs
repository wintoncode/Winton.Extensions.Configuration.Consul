using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using FluentAssertions;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Winton.Extensions.Configuration.Consul
{
    public class ConsulConfigurationClientTests : IDisposable
    {
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConsulConfigurationClient _consulConfigurationClient;
        private readonly Mock<IKVEndpoint> _kvMock;

        private ConsulConfigurationClientTests()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            var consulConfigurationSourceMock = new Mock<IConsulConfigurationSource>(MockBehavior.Strict);
            consulConfigurationSourceMock.Setup(ccs => ccs.CancellationToken).Returns(_cancellationToken);
            consulConfigurationSourceMock.Setup(ccs => ccs.Key).Returns("Test");

            _kvMock = new Mock<IKVEndpoint>(MockBehavior.Strict);

            var consulClientMock = new Mock<IConsulClient>(MockBehavior.Strict);
            consulClientMock.Setup(cc => cc.Dispose());
            consulClientMock.Setup(cc => cc.KV).Returns(_kvMock.Object);
            var consulClientFactoryMock = new Mock<IConsulClientFactory>(MockBehavior.Strict);
            consulClientFactoryMock.Setup(ccf => ccf.Create()).Returns(consulClientMock.Object);

            _consulConfigurationClient = new ConsulConfigurationClient(
                consulClientFactoryMock.Object,
                consulConfigurationSourceMock.Object);
        }

        public void Dispose()
        {
            // Ensure any background threads that were started are stopped
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        public sealed class GetConfig : ConsulConfigurationClientTests
        {
            [Fact]
            private async Task ShouldGetConfigAtSpecifiedKey()
            {
                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .ReturnsAsync(
                        new QueryResult<KVPair>
                        {
                            StatusCode = HttpStatusCode.OK
                        });

                await _consulConfigurationClient.GetConfig();

                Action verifying = () => _kvMock.Verify(kv => kv.Get("Test", null, _cancellationToken), Times.Once);
                verifying.ShouldNotThrow();
            }

            [Theory]
            [InlineData(HttpStatusCode.OK)]
            [InlineData(HttpStatusCode.NotFound)]
            private async Task ShouldReturnResultWhenValidResponse(HttpStatusCode statusCode)
            {
                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .ReturnsAsync(
                        new QueryResult<KVPair>
                        {
                            StatusCode = statusCode
                        });

                QueryResult<KVPair> queryResult = await _consulConfigurationClient.GetConfig();

                queryResult.Should().NotBeNull();
            }

            [Fact]
            private void ShouldThrowExceptionWhenBadResponseRecieved()
            {
                _kvMock.Setup(kv => kv.Get("Test", null, _cancellationToken))
                       .ReturnsAsync(
                           new QueryResult<KVPair>
                           {
                               StatusCode = HttpStatusCode.BadRequest
                           });

                Func<Task> gettingConfig = _consulConfigurationClient.Awaiting(ccc => ccc.GetConfig());

                gettingConfig.ShouldThrow<Exception>()
                             .WithMessage("Error loading configuration from consul. Status code: BadRequest.");
            }

            [Fact]
            private void ShouldThrowExceptionWhenExceptionalResponseRecieved()
            {
                _kvMock
                    .Setup(kv => kv.Get("Test", null, _cancellationToken))
                    .ThrowsAsync(new WebException("Failed to connect to Consul client"));

                Func<Task> gettingConfig = _consulConfigurationClient.Awaiting(ccc => ccc.GetConfig());

                gettingConfig.ShouldThrow<WebException>().WithMessage("Failed to connect to Consul client");
            }
        }

        public sealed class Watch : ConsulConfigurationClientTests
        {
            [Fact]
            private async Task ShouldCallReloadOnChangeTokenIfIndexForKeyHasUpdated()
            {
                var configChangedCompletion = new TaskCompletionSource<bool>();
                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .ReturnsAsync(
                        new QueryResult<KVPair>
                        {
                            LastIndex = 1,
                            StatusCode = HttpStatusCode.OK
                        });
                ChangeToken.OnChange(
                    () => _consulConfigurationClient.Watch(null),
                    () => configChangedCompletion.SetResult(true));

                bool completed = await configChangedCompletion.Task;

                completed.Should().BeTrue();
            }

            [Fact]
            private async Task ShouldInvokeExceptionActionWhenWatchThrowsException()
            {
                Exception actualException = null;
                var expectedException = new Exception();
                var configChangedCompletion = new TaskCompletionSource<bool>();

                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .ThrowsAsync(expectedException);

                _consulConfigurationClient.Watch(
                    exceptionContext =>
                    {
                        actualException = exceptionContext.Exception;
                        _cancellationTokenSource.Cancel();
                        configChangedCompletion.SetResult(true);
                    });

                await configChangedCompletion.Task;

                actualException.Should().BeSameAs(expectedException);
            }

            [Fact]
            private void ShouldUseLongPollingToPollForChanges()
            {
                Func<Task<bool>> watching = () => SimulateConfigChange(1);

                watching.ShouldNotThrow();
            }

            [Fact]
            private async Task ShouldUseLongPollingWithLatestIndexFromGet()
            {
                ulong lastWaitIndex = 0;
                const ulong lastIndex = 1;
                var completion = new TaskCompletionSource<bool>();

                // Get config once which should update the latest index
                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .ReturnsAsync(
                        new QueryResult<KVPair>
                        {
                            LastIndex = 1,
                            StatusCode = HttpStatusCode.OK
                        });

                await _consulConfigurationClient.GetConfig();

                var result = new QueryResult<KVPair>
                {
                    LastIndex = lastIndex + 1,
                    StatusCode = HttpStatusCode.OK
                };
                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .Callback(
                        (string key, QueryOptions options, CancellationToken cancellationToken) =>
                        {
                            lastWaitIndex = options.WaitIndex;
                        })
                    .ReturnsAsync(result);

                // Calling it a second time should invoke a long polling with the index from the last update
                _consulConfigurationClient.Watch(null).RegisterChangeCallback(
                    o => completion.SetResult(true),
                    new object());

                await completion.Task;

                lastWaitIndex.Should().Be(lastIndex);
            }

            [Fact]
            private async Task ShouldUseLongPollingWithWaitIndexFromPreviousWatch()
            {
                ulong lastWaitIndex = 0;
                const ulong lastIndex = 1;
                var completion = new TaskCompletionSource<bool>();

                // Simulate the first change in config which generates a new index
                await SimulateConfigChange(lastIndex);

                var result = new QueryResult<KVPair>
                {
                    LastIndex = lastIndex + 1,
                    StatusCode = HttpStatusCode.OK
                };
                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .Callback(
                        (string key, QueryOptions options, CancellationToken cancellationToken) =>
                        {
                            lastWaitIndex = options.WaitIndex;
                        })
                    .ReturnsAsync(result);

                // Calling it a second time should invoke a long polling with the index from the last update
                _consulConfigurationClient.Watch(null).RegisterChangeCallback(
                    o => completion.SetResult(true),
                    new object());

                await completion.Task;

                lastWaitIndex.Should().Be(lastIndex);
            }

            private async Task<bool> SimulateConfigChange(ulong lastIndex)
            {
                var configChangedCompletion = new TaskCompletionSource<bool>();

                // Initially setup mock with 0 last index so that no changes occur
                var result = new QueryResult<KVPair>
                {
                    LastIndex = 0,
                    StatusCode = HttpStatusCode.OK
                };
                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .ReturnsAsync(result);

                // Watch for changes
                _consulConfigurationClient
                    .Watch(
                        exceptionContext =>
                        {
                            _cancellationTokenSource.Cancel();
                            configChangedCompletion.SetException(exceptionContext.Exception);
                        })
                    .RegisterChangeCallback(
                        o => configChangedCompletion.SetResult(true),
                        new object());

                // Update mocked result to return a higher last index, which is what happens when changes occur
                result.LastIndex = lastIndex;
                _kvMock
                    .Setup(kv => kv.Get("Test", It.IsAny<QueryOptions>(), _cancellationToken))
                    .ReturnsAsync(result);

                return await configChangedCompletion.Task;
            }
        }
    }
}