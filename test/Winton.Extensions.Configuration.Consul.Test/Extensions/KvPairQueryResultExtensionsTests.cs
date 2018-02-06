using System.Net;
using Consul;
using NUnit.Framework;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    [TestFixture]
    public class KvPairQueryResultExtensionsTests
    {
        public sealed class HasValue : KvPairQueryResultExtensionsTests
        {
            [TestCase]
            public void ShouldBeFalseWhenNull()
            {
                var queryResult = null as QueryResult<KVPair>;

                // ReSharper disable once ExpressionIsAlwaysNull
                bool hasValue = queryResult.HasValue();

                Assert.That(hasValue, Is.False);
            }

            [TestCase]
            public void ShouldBeFalseWhenResponseHasEmptyValue()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    Response = new KVPair("Key")
                    {
                        Value = new byte[]
                        {
                        }
                    },
                    StatusCode = HttpStatusCode.OK
                };

                bool hasValue = queryResult.HasValue();

                Assert.That(hasValue, Is.False);
            }

            [TestCase]
            public void ShouldBeFalseWhenResponseHasNullValue()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    Response = new KVPair("Key")
                    {
                        Value = null
                    },
                    StatusCode = HttpStatusCode.OK
                };

                bool hasValue = queryResult.HasValue();

                Assert.That(hasValue, Is.False);
            }

            [TestCase]
            public void ShouldBeFalseWhenResponseIsNull()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    Response = null,
                    StatusCode = HttpStatusCode.OK
                };

                bool hasValue = queryResult.HasValue();

                Assert.That(hasValue, Is.False);
            }

            [TestCase]
            public void ShouldBeFalseWhenStatusIsNotFound()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    StatusCode = HttpStatusCode.NotFound
                };

                bool hasValue = queryResult.HasValue();

                Assert.That(hasValue, Is.False);
            }

            [TestCase]
            public void ShouldSetExistsToTrueWhenResultHasValue()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    Response = new KVPair("Key")
                    {
                        Value = new byte[]
                        {
                            1
                        }
                    },
                    StatusCode = HttpStatusCode.OK
                };

                bool hasValue = queryResult.HasValue();

                Assert.That(hasValue, Is.True);
            }
        }

        public sealed class Value : KvPairQueryResultExtensionsTests
        {
            [TestCase]
            public void ShouldBeNullIfResponseIsNull()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    StatusCode = HttpStatusCode.OK
                };

                byte[] value = queryResult.Value();

                Assert.That(value, Is.Null);
            }

            [TestCase]
            public void ShouldReturnResponseValue()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    Response = new KVPair("Key")
                    {
                        Value = new byte[]
                        {
                            1
                        }
                    },
                    StatusCode = HttpStatusCode.OK
                };

                byte[] value = queryResult.Value();

                Assert.That(
                    value,
                    Is.EquivalentTo(
                        new byte[]
                        {
                            1
                        }));
            }
        }
    }
}