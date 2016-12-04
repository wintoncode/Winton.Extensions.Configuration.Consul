using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    [TestFixture]
    internal sealed class ConsulConfigurationClientTests
    {
        private const string _Key = "Key/Test";

        private ConsulConfigurationClient _consulConfigurationClient;
        private Mock<IConsulClientFactory> _consulClientFactoryMock;
        private Mock<IConsulClient> _consulClientMock;
        private Mock<IConsulConfigurationSource> _consulConfigurationSourceMock;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private Mock<IKVEndpoint> _kvMock;

        [SetUp]
        public void SetUp()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _consulConfigurationSourceMock = new Mock<IConsulConfigurationSource>(MockBehavior.Strict);
            _consulConfigurationSourceMock.SetupGet(ccs => ccs.CancellationToken).Returns(_cancellationToken);
            _consulConfigurationSourceMock.SetupGet(ccs => ccs.Key).Returns(_Key);

            _kvMock = new Mock<IKVEndpoint>(MockBehavior.Strict);

            _consulClientMock = new Mock<IConsulClient>(MockBehavior.Strict);
            _consulClientMock.Setup(cc => cc.Dispose());
            _consulClientMock.SetupGet(cc => cc.KV).Returns(_kvMock.Object);
            _consulClientFactoryMock = new Mock<IConsulClientFactory>(MockBehavior.Strict);
            _consulClientFactoryMock.Setup(ccf => ccf.Create()).Returns(_consulClientMock.Object);

            _consulConfigurationClient = new ConsulConfigurationClient(_consulClientFactoryMock.Object, _consulConfigurationSourceMock.Object);
        }

        [Test]
        public async Task ShouldGetConfigAtSpecifiedKey()
        {
            var result = new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.OK
            };
            await SimulateGet(result);

            Assert.That(() => _kvMock.Verify(kv => kv.Get(_Key, null, _cancellationToken)), Throws.Nothing);
        }

        [Test]
        [TestCase(HttpStatusCode.OK, TestName = "ShouldReturnResultWhenGetConfigReceivesOKResponse")]
        [TestCase(HttpStatusCode.NotFound, TestName = "ShouldReturnResultWhenGetConfigReceivesNotFoundResponse")]
        public async Task ShouldReturnResultWhenGetConfigReceivesValidResponse(HttpStatusCode statusCode)
        {
            var result = new QueryResult<KVPair>
            {
                StatusCode = statusCode
            };
            IConfigQueryResult configQueryResult = await SimulateGet(result);

            Assert.That(configQueryResult, Is.Not.Null);
        }

        [Test]
        public void ShouldThrowExceptionWhenGetConfigReceivesABadResponse()
        {
            var result = new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.BadRequest
            };
            _kvMock.Setup(kv => kv.Get(_Key, null, _cancellationToken)).ReturnsAsync(result);

            Assert.ThrowsAsync<Exception>(_consulConfigurationClient.GetConfig);
        }

        [Test]
        public void ShouldThrowExceptionWhenGetConfigRecievesAnExceptionalResponse()
        {
            var exception = new WebException("Failed to connect to Consul client");
            _kvMock
                .Setup(kv => kv.Get(_Key, null, _cancellationToken))
                .ThrowsAsync(exception);

            Assert.ThrowsAsync<WebException>(_consulConfigurationClient.GetConfig);
        }

        [Test]
        public async Task ShouldUseLongPollingToPollForChangesWhenWatch()
        {
            await SimulateConfigChange(1);

            Assert.That(
                () => _kvMock.Verify(
                    kv => kv.Get(_Key, It.IsAny<QueryOptions>(), _cancellationToken),
                    Times.Once), 
                Throws.Nothing);
        }

        [Test]
        public async Task ShouldUseLongPollingWithLatestIndexFromGetWhenWatch()
        {
            ulong lastWaitIndex = 0;
            ulong lastIndex = 1;
            var completion = new TaskCompletionSource<bool>();

            // Get config once which should update the latest index
            await SimulateGet(new QueryResult<KVPair>
            {
                LastIndex = 1,
                StatusCode = HttpStatusCode.OK
            });

            var result = new QueryResult<KVPair>
            {
                LastIndex = lastIndex + 1,
                StatusCode = HttpStatusCode.OK
            };
            _kvMock
                .Setup(kv => kv.Get(_Key, It.IsAny<QueryOptions>(), _cancellationToken))
                .Callback((string key, QueryOptions options, CancellationToken cancellationToken) => 
                {
                    lastWaitIndex = options.WaitIndex;
                })
                .ReturnsAsync(result);

            // Calling it a second time should invoke a long polling with the index from the last update
            _consulConfigurationClient.Watch(null).RegisterChangeCallback(
                o => completion.SetResult(true),
                new object());

            await completion.Task;
            Assert.That(lastWaitIndex, Is.EqualTo(lastIndex));
        }

        [Test]
        public async Task ShouldUseLongPollingWithWaitIndexFromPreviousWatchWhenWatch()
        {
            ulong lastWaitIndex = 0;
            ulong lastIndex = 1;
            var completion = new TaskCompletionSource<bool>();

            // Simulate the first change in config which generates a new index
            await SimulateConfigChange(lastIndex);

            var result = new QueryResult<KVPair>
            {
                LastIndex = lastIndex + 1,
                StatusCode = HttpStatusCode.OK
            };
            _kvMock
                .Setup(kv => kv.Get(_Key, It.IsAny<QueryOptions>(), _cancellationToken))
                .Callback((string key, QueryOptions options, CancellationToken cancellationToken) => 
                {
                    lastWaitIndex = options.WaitIndex;
                })
                .ReturnsAsync(result);

            // Calling it a second time should invoke a long polling with the index from the last update
            _consulConfigurationClient.Watch(null).RegisterChangeCallback(
                o => completion.SetResult(true),
                new object());

            await completion.Task;
            Assert.That(lastWaitIndex, Is.EqualTo(lastIndex));
        }

        [Test]
        public async Task ShouldCallReloadOnChangeTokenIfIndexForKeyHasUpdatedWhenWatch()
        {
            var configChangedCompletion = new TaskCompletionSource<bool>();
            var result = new QueryResult<KVPair>
            {
                LastIndex = 1,
                StatusCode = HttpStatusCode.OK
            };
            _kvMock
                .Setup(kv => kv.Get(_Key, It.IsAny<QueryOptions>(), _cancellationToken))
                .ReturnsAsync(result);

            ChangeToken.OnChange(
                () => _consulConfigurationClient.Watch(null),
                () => configChangedCompletion.SetResult(true));

            Assert.That(await configChangedCompletion.Task, Is.True);
        }

        [Test]
        public async Task ShouldInvokeExceptionActionWhenWatchThrowsException()
        {
            Exception actualException = null;
            Exception expectedException = new Exception();
            var configChangedCompletion = new TaskCompletionSource<bool>();
            
            _kvMock
                .Setup(kv => kv.Get(_Key, It.IsAny<QueryOptions>(), _cancellationToken))
                .ThrowsAsync(expectedException);

            _consulConfigurationClient.Watch(exceptionContext => 
            {
                actualException = exceptionContext.Exception;
                _cancellationTokenSource.Cancel();
                configChangedCompletion.SetResult(true);
            });

            await configChangedCompletion.Task;

            Assert.That(actualException, Is.SameAs(expectedException));
        }

        private async Task<IConfigQueryResult> SimulateGet(QueryResult<KVPair> result)
        {
            _kvMock
                .Setup(kv => kv.Get(_Key, It.IsAny<QueryOptions>(), _cancellationToken))
                .ReturnsAsync(result);
            _consulClientMock.SetupGet(cc => cc.KV).Returns(_kvMock.Object);

            return await _consulConfigurationClient.GetConfig();
        }

        private async Task<bool> SimulateConfigChange(ulong lastIndex)
        {
            var configChangedCompletion = new TaskCompletionSource<bool>();
            var result = new QueryResult<KVPair>
            {
                LastIndex = lastIndex,
                StatusCode = HttpStatusCode.OK
            };
            _kvMock
                .Setup(kv => kv.Get(_Key, It.IsAny<QueryOptions>(), _cancellationToken))
                .ReturnsAsync(result);

            _consulConfigurationClient.Watch(null).RegisterChangeCallback(
                o => configChangedCompletion.SetResult(true),
                new object());

            return await configChangedCompletion.Task;
        }
    }
}