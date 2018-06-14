using System.Collections.Generic;
using System.Net;
using Consul;
using FluentAssertions;
using Xunit;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    public class KVPairQueryResultExtensionsTests
    {
        public sealed class HasValue : KVPairQueryResultExtensionsTests
        {
            public static IEnumerable<object[]> TestCases => new List<object[]>
            {
                new object[]
                {
                    null as QueryResult<KVPair[]>,
                    false
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        StatusCode = HttpStatusCode.NotFound
                    },
                    false
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        Response = null,
                        StatusCode = HttpStatusCode.OK
                    },
                    false
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        Response = new KVPair[] { },
                        StatusCode = HttpStatusCode.OK
                    },
                    false
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        Response = new[]
                        {
                            new KVPair("Key")
                            {
                                Value = null
                            }
                        },
                        StatusCode = HttpStatusCode.OK
                    },
                    false
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        Response = new[]
                        {
                            new KVPair("Key/")
                        },
                        StatusCode = HttpStatusCode.OK
                    },
                    false
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        Response = new[]
                        {
                            new KVPair("Key")
                            {
                                Value = new byte[]
                                {
                                }
                            }
                        },
                        StatusCode = HttpStatusCode.OK
                    },
                    false
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        Response = new[]
                        {
                            new KVPair("Key")
                            {
                                Value = new byte[]
                                {
                                    1
                                }
                            }
                        },
                        StatusCode = HttpStatusCode.OK
                    },
                    true
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        Response = new[]
                        {
                            new KVPair("Key1")
                            {
                                Value = new byte[]
                                {
                                    1
                                }
                            },
                            new KVPair("Key2")
                        },
                        StatusCode = HttpStatusCode.OK
                    },
                    true
                },
                new object[]
                {
                    new QueryResult<KVPair[]>
                    {
                        Response = new[]
                        {
                            new KVPair("Key1")
                            {
                                Value = new byte[]
                                {
                                    1
                                }
                            },
                            new KVPair("Key2/")
                        },
                        StatusCode = HttpStatusCode.OK
                    },
                    true
                }
            };

            [Theory]
            [MemberData(nameof(TestCases))]
            private void ShouldBeTrueWhenThereIsAtLeastOneLeafNodeWithSomeData(QueryResult<KVPair[]> queryResult, bool expected)
            {
                bool hasValue = queryResult.HasValue();

                var dict = new Dictionary<string, string>();

                hasValue.Should().Be(expected);
            }
        }
    }
}