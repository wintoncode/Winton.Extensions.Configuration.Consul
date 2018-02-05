using System.Collections.Generic;
using System.Net;
using Consul;
using FluentAssertions;
using Xunit;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    public class KvPairQueryResultExtensionsTests
    {
        public sealed class HasValue : KvPairQueryResultExtensionsTests
        {
            [Fact]
            private void ShouldBeFalseWhenNull()
            {
                var queryResult = null as QueryResult<KVPair>;

                // ReSharper disable once ExpressionIsAlwaysNull
                bool hasValue = queryResult.HasValue();

                hasValue.Should().BeFalse();
            }

            [Fact]
            private void ShouldBeFalseWhenResponseHasEmptyValue()
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

                hasValue.Should().BeFalse();
            }

            [Fact]
            private void ShouldBeFalseWhenResponseHasNullValue()
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

                hasValue.Should().BeFalse();
            }

            [Fact]
            private void ShouldBeFalseWhenResponseIsNull()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    Response = null,
                    StatusCode = HttpStatusCode.OK
                };

                bool hasValue = queryResult.HasValue();

                hasValue.Should().BeFalse();
            }

            [Fact]
            private void ShouldBeFalseWhenStatusIsNotFound()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    StatusCode = HttpStatusCode.NotFound
                };

                bool hasValue = queryResult.HasValue();

                hasValue.Should().BeFalse();
            }

            [Fact]
            private void ShouldBeToTrueWhenResultHasValue()
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

                hasValue.Should().BeTrue();
            }
        }

        public sealed class Value : KvPairQueryResultExtensionsTests
        {
            [Fact]
            private void ShouldBeNullIfResponseIsNull()
            {
                var queryResult = new QueryResult<KVPair>
                {
                    StatusCode = HttpStatusCode.OK
                };

                byte[] value = queryResult.Value();

                value.Should().BeNull();
            }

            [Fact]
            private void ShouldReturnResponseValue()
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

                value.Should().BeEquivalentTo(new List<byte> { 1 }.ToArray());
            }
        }
    }
}