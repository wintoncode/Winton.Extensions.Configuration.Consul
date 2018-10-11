using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ConsulConfigurationClientTests
    {
        private readonly ConsulConfigurationClient _consulConfigurationClient;
        private readonly Mock<IKVEndpoint> _kvMock;

        private ConsulConfigurationClientTests()
        {
            _kvMock = new Mock<IKVEndpoint>(MockBehavior.Strict);

            var consulClientMock = new Mock<IConsulClient>(MockBehavior.Strict);
            consulClientMock.Setup(cc => cc.Dispose());
            consulClientMock.Setup(cc => cc.KV).Returns(_kvMock.Object);
            var consulClientFactoryMock = new Mock<IConsulClientFactory>(MockBehavior.Strict);
            consulClientFactoryMock.Setup(ccf => ccf.Create()).Returns(consulClientMock.Object);

            _consulConfigurationClient = new ConsulConfigurationClient(consulClientFactoryMock.Object);
        }

        public sealed class GetConfig : ConsulConfigurationClientTests
        {
            [Fact]
            private async Task ShouldGetConfigAtSpecifiedKey()
            {
                _kvMock
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), default(CancellationToken)))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            StatusCode = HttpStatusCode.OK
                        });

                await _consulConfigurationClient.GetConfig("Test", default(CancellationToken));

                Action verifying =
                    () => _kvMock.Verify(kv => kv.List("Test", null, default(CancellationToken)), Times.Once);
                verifying.Should().NotThrow();
            }

            [Theory]
            [InlineData(HttpStatusCode.OK)]
            [InlineData(HttpStatusCode.NotFound)]
            private async Task ShouldReturnResultWhenValidResponse(HttpStatusCode statusCode)
            {
                _kvMock
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), default(CancellationToken)))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            StatusCode = statusCode
                        });

                QueryResult<KVPair[]> queryResult = await _consulConfigurationClient
                    .GetConfig("Test", default(CancellationToken));

                queryResult.Should().NotBeNull();
            }

            [Fact]
            private void ShouldThrowExceptionWhenBadResponseRecieved()
            {
                _kvMock
                    .Setup(kv => kv.List("Test", null, default(CancellationToken)))
                    .ReturnsAsync(
                        new QueryResult<KVPair[]>
                        {
                            StatusCode = HttpStatusCode.BadRequest
                        });

                Func<Task> gettingConfig = _consulConfigurationClient
                    .Awaiting(ccc => ccc.GetConfig("Test", default(CancellationToken)));

                gettingConfig
                    .Should()
                    .Throw<Exception>()
                    .WithMessage("Error loading configuration from consul. Status code: BadRequest.");
            }

            [Fact]
            private void ShouldThrowExceptionWhenExceptionalResponseRecieved()
            {
                _kvMock
                    .Setup(kv => kv.List("Test", null, default(CancellationToken)))
                    .ThrowsAsync(new WebException("Failed to connect to Consul client"));

                Func<Task> gettingConfig = _consulConfigurationClient
                    .Awaiting(ccc => ccc.GetConfig("Test", default(CancellationToken)));

                gettingConfig.Should().Throw<WebException>().WithMessage("Failed to connect to Consul client");
            }
        }

        public sealed class Watch : ConsulConfigurationClientTests
        {
            [Fact]
            private async Task ShouldCallReloadOnChangeTokenIfIndexForKeyHasUpdated()
            {
                var configChangedCompletion = new TaskCompletionSource<bool>();
                var getKvTaskSource = new TaskCompletionSource<QueryResult<KVPair[]>>();
                _kvMock
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), default(CancellationToken)))
                    .Returns(getKvTaskSource.Task);

                ChangeToken.OnChange(
                    () => _consulConfigurationClient.Watch("Test", null, default(CancellationToken)),
                    () => configChangedCompletion.SetResult(true));

                getKvTaskSource.SetResult(
                    new QueryResult<KVPair[]>
                    {
                        LastIndex = 1,
                        StatusCode = HttpStatusCode.OK
                    });
                bool completed = await configChangedCompletion.Task;

                completed.Should().BeTrue();
            }

            [Fact]
            private async Task ShouldInvokeExceptionActionWhenWatchThrowsException()
            {
                Exception actualException = null;
                var expectedException = new Exception();
                var configChangedCompletion = new TaskCompletionSource<bool>();
                var getKvTaskSource = new TaskCompletionSource<QueryResult<KVPair[]>>();

                _kvMock
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), default(CancellationToken)))
                    .Returns(getKvTaskSource.Task);

                _consulConfigurationClient.Watch(
                    "Test",
                    exceptionContext =>
                    {
                        actualException = exceptionContext.Exception;
                        configChangedCompletion.SetResult(true);
                    },
                    default(CancellationToken));

                getKvTaskSource.SetException(expectedException);
                await configChangedCompletion.Task;

                actualException.Should().BeSameAs(expectedException);
            }

            [Fact]
            private async Task ShouldUseLongPollingToPollForChanges()
            {
                TaskCompletionSource<QueryResult<KVPair[]>>[] kvTaskSources =
                    Enumerable.Range(0, 10).Select(i => new TaskCompletionSource<QueryResult<KVPair[]>>()).ToArray();
                var kvTaskQueue = new Queue<Task<QueryResult<KVPair[]>>>(kvTaskSources.Select(kts => kts.Task));

                _kvMock
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), default(CancellationToken)))
                    .Returns(() => kvTaskQueue.Dequeue());

                var watchCompletion = new TaskCompletionSource<bool>();
                _consulConfigurationClient
                    .Watch(
                        "Test",
                        exceptionContext =>
                        {
                            watchCompletion.SetResult(false);
                            throw exceptionContext.Exception;
                        },
                        default(CancellationToken))
                    .RegisterChangeCallback(o => watchCompletion.SetResult(true), new object());

                // The first 5 long polling calls return an unchanged last index
                foreach (int i in Enumerable.Range(0, 5))
                {
                    kvTaskSources[i].SetResult(
                        new QueryResult<KVPair[]>
                        {
                            LastIndex = 0,
                            StatusCode = HttpStatusCode.OK
                        });
                }

                // The 6th call returns an updated index indicating that the config has changed
                kvTaskSources[5].SetResult(
                    new QueryResult<KVPair[]>
                    {
                        LastIndex = 1,
                        StatusCode = HttpStatusCode.OK
                    });

                await watchCompletion.Task;

                Action verifying = () => _kvMock
                    .Verify(
                        kv => kv.List("Test", It.IsAny<QueryOptions>(), default(CancellationToken)),
                        Times.Exactly(6));
            }

            [Fact]
            private async Task ShouldUseLongPollingWithLatestIndexFromGet()
            {
                ulong? watchWaitIndex = 0;
                var getKvTaskSource = new TaskCompletionSource<QueryResult<KVPair[]>>();
                var watchKvTaskSource = new TaskCompletionSource<QueryResult<KVPair[]>>();
                var kvTaskQueue = new Queue<Task<QueryResult<KVPair[]>>>(
                    new List<Task<QueryResult<KVPair[]>>> { getKvTaskSource.Task, watchKvTaskSource.Task });

                _kvMock
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), default(CancellationToken)))
                    .Returns(
                        (string key, QueryOptions options, CancellationToken cancellationToken) =>
                        {
                            watchWaitIndex = options?.WaitIndex;
                            return kvTaskQueue.Dequeue();
                        });

                getKvTaskSource.SetResult(
                    new QueryResult<KVPair[]>
                    {
                        LastIndex = 1,
                        StatusCode = HttpStatusCode.OK
                    });

                // Get config once which should update the latest index
                await _consulConfigurationClient.GetConfig("Test", default(CancellationToken));

                // Calling it a second time should invoke a long polling with the index from the previous get call
                var watchCompletion = new TaskCompletionSource<bool>();
                _consulConfigurationClient
                    .Watch("Test", null, default(CancellationToken))
                    .RegisterChangeCallback(o => watchCompletion.SetResult(true), new object());

                watchKvTaskSource.SetResult(
                    new QueryResult<KVPair[]>
                    {
                        LastIndex = 2,
                        StatusCode = HttpStatusCode.OK
                    });
                await watchCompletion.Task;

                watchWaitIndex.Should().Be(1);
            }

            [Fact]
            private async Task ShouldUseLongPollingWithWaitIndexFromPreviousWatch()
            {
                ulong? waitIndex = 0;
                var watchKvTaskSource1 = new TaskCompletionSource<QueryResult<KVPair[]>>();
                var watchKvTaskSource2 = new TaskCompletionSource<QueryResult<KVPair[]>>();
                var kvTaskQueue = new Queue<Task<QueryResult<KVPair[]>>>(
                    new List<Task<QueryResult<KVPair[]>>> { watchKvTaskSource1.Task, watchKvTaskSource2.Task });

                _kvMock
                    .Setup(kv => kv.List("Test", It.IsAny<QueryOptions>(), default(CancellationToken)))
                    .Returns(
                        (string key, QueryOptions options, CancellationToken cancellationToken) =>
                        {
                            waitIndex = options?.WaitIndex;
                            return kvTaskQueue.Dequeue();
                        });

                // The KV result initiated by the first watch returns with an updated index of 1
                var watchCompletion1 = new TaskCompletionSource<bool>();
                _consulConfigurationClient
                    .Watch("Test", null, default(CancellationToken))
                    .RegisterChangeCallback(o => watchCompletion1.SetResult(true), new object());

                watchKvTaskSource1.SetResult(
                    new QueryResult<KVPair[]>
                    {
                        LastIndex = 1,
                        StatusCode = HttpStatusCode.OK
                    });
                await watchCompletion1.Task;

                // The KV result from the second watch returns with an updated index so that it can be determined that it ran inside the watch
                var watchCompletion2 = new TaskCompletionSource<bool>();
                _consulConfigurationClient
                    .Watch("Test", null, default(CancellationToken))
                    .RegisterChangeCallback(o => watchCompletion2.SetResult(true), new object());

                watchKvTaskSource2.SetResult(
                    new QueryResult<KVPair[]>
                    {
                        LastIndex = 2,
                        StatusCode = HttpStatusCode.OK
                    });
                await watchCompletion2.Task;

                // The wait index sent the second time should be the value returned from the first KV result
                waitIndex.Should().Be(1);
            }
        }
    }
}