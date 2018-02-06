using System.Net;
using Consul;
using NUnit.Framework;

namespace Winton.Extensions.Configuration.Consul
{
    [TestFixture]
    internal sealed class ConfigQueryResultTests
    {
        [Test]
        public void ShouldSetExistsToFalseWhenConstructedFromNullResult()
        {
            var configQueryResult = new ConfigQueryResult(null);

            Assert.That(configQueryResult.Exists, Is.False);
        }

        [Test]
        public void ShouldSetExistsToFalseWhenConstructedFromResultWithEmptyValue()
        {
            var kvPairResult = new QueryResult<KVPair>
            {
                Response = new KVPair("Key")
                {
                    Value = new byte[]
                    {
                    }
                },
                StatusCode = HttpStatusCode.OK
            };

            var configQueryResult = new ConfigQueryResult(kvPairResult);

            Assert.That(configQueryResult.Exists, Is.False);
        }

        [Test]
        public void ShouldSetExistsToFalseWhenConstructedFromResultWithNotFoundStatus()
        {
            var kvPairResult = new QueryResult<KVPair>
            {
                StatusCode = HttpStatusCode.NotFound
            };

            var configQueryResult = new ConfigQueryResult(kvPairResult);

            Assert.That(configQueryResult.Exists, Is.False);
        }

        [Test]
        public void ShouldSetExistsToFalseWhenConstructedFromResultWithNullResponse()
        {
            var kvPairResult = new QueryResult<KVPair>
            {
                Response = null,
                StatusCode = HttpStatusCode.OK
            };

            var configQueryResult = new ConfigQueryResult(kvPairResult);

            Assert.That(configQueryResult.Exists, Is.False);
        }

        [Test]
        public void ShouldSetExistsToFalseWhenConstructedFromResultWithNullValue()
        {
            var kvPairResult = new QueryResult<KVPair>
            {
                Response = new KVPair("Key")
                {
                    Value = null
                },
                StatusCode = HttpStatusCode.OK
            };

            var configQueryResult = new ConfigQueryResult(kvPairResult);

            Assert.That(configQueryResult.Exists, Is.False);
        }

        [Test]
        public void ShouldSetValueToResultValue()
        {
            var actualValue = new byte[]
            {
                1
            };
            var kvPairResult = new QueryResult<KVPair>
            {
                Response = new KVPair("Key")
                {
                    Value = actualValue
                },
                StatusCode = HttpStatusCode.OK
            };

            var configQueryResult = new ConfigQueryResult(kvPairResult);

            Assert.That(configQueryResult.Value, Is.SameAs(actualValue));
        }
    }
}