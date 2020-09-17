using System;
using System.Collections.Generic;
using System.IO;
using Consul;
using FluentAssertions;
using Moq;
using Winton.Extensions.Configuration.Consul.Parsers;
using Xunit;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    public class KVPairExtensionsTests
    {
        public sealed class ConvertConsulKVPairToConfig : KVPairExtensionsTests
        {
            private readonly Mock<IConfigurationParser> _parserMock;

            public ConvertConsulKVPairToConfig()
            {
                _parserMock = new Mock<IConfigurationParser>(MockBehavior.Strict);
            }

            public static IEnumerable<object[]> ConvertConsulKVPairToConfigTestCases => new List<object[]>
            {
                new object[]
                {
                    "rootKey",
                    "rootKey",
                    new Dictionary<string, string> { { "Key", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Key", "value")
                    }
                },
                new object[]
                {
                    "rootKey",
                    "rootKey/",
                    new Dictionary<string, string> { { "Key", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Key", "value")
                    }
                },
                new object[]
                {
                    "rootKey",
                    "rootKey/Key",
                    new Dictionary<string, string> { { string.Empty, "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Key", "value")
                    }
                },
                new object[]
                {
                    "RootKey/Settings",
                    "RootKey/Settings/Root/",
                    new Dictionary<string, string> { { "Key", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Root:Key", "value")
                    }
                },
                new object[]
                {
                    "rootKey",
                    "rootKey",
                    new Dictionary<string, string> { { "Section:Property", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Section:Property", "value")
                    }
                },
                new object[]
                {
                    "rootKey",
                    "rootKey/Section",
                    new Dictionary<string, string> { { "SubSection:Property", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Section:SubSection:Property", "value")
                    }
                },
                new object[]
                {
                    "rootKey",
                    "rootKey/Section/SubSection",
                    new Dictionary<string, string> { { "Property", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Section:SubSection:Property", "value")
                    }
                },
                new object[]
                {
                    "rootKey",
                    "rootKey/Section/SubSection",
                    new Dictionary<string, string> { { "Property", "1" }, { "AnotherSubSection:Property", "2" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Section:SubSection:Property", "1"),
                        new KeyValuePair<string, string>("Section:SubSection:AnotherSubSection:Property", "2")
                    }
                },
                new object[]
                {
                    "path/to/rootKey",
                    "path/to/rootKey",
                    new Dictionary<string, string> { { "Key", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Key", "value")
                    }
                },
                new object[]
                {
                    "path/to/rootKey",
                    "path/to/rootKey/",
                    new Dictionary<string, string> { { "Key", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Key", "value")
                    }
                },
                new object[]
                {
                    "path/to/rootKey",
                    "path/to/rootKey/Section",
                    new Dictionary<string, string> { { "Key", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Section:Key", "value")
                    }
                },
                new object[]
                {
                    string.Empty,
                    "Root/Section",
                    new Dictionary<string, string> { { "JsonKey/With/Slash", "value" } },
                    new[]
                    {
                        new KeyValuePair<string, string>("Root:Section:JsonKey/With/Slash", "value")
                    }
                }
            };

            [Theory]
            [MemberData(nameof(ConvertConsulKVPairToConfigTestCases))]
            private void ShouldConvertKVPairToConfigCorrectly(
                string rootKey,
                string kvPairKey,
                Dictionary<string, string> parsedConfig,
                IEnumerable<KeyValuePair<string, string>> expected)
            {
                _parserMock
                    .Setup(p => p.Parse(It.IsAny<Stream>()))
                    .Returns(parsedConfig);
                var kvPair = new KVPair(kvPairKey)
                {
                    Value = new byte[]
                    {
                        1
                    }
                };

                var config = kvPair.ConvertToConfig(rootKey, _parserMock.Object);

                config.Should().BeEquivalentTo(expected);
            }

            [Fact]
            private void ShouldThrowIfTheRootKeyPointsToASingleValue()
            {
                _parserMock
                    .Setup(p => p.Parse(It.IsAny<Stream>()))
                    .Returns(new Dictionary<string, string> { { string.Empty, "value" } });
                var kvPair = new KVPair("rootKey")
                {
                    Value = new byte[]
                    {
                        1
                    }
                };

                Func<IEnumerable<KeyValuePair<string, string>>> converting =
                    () => kvPair.ConvertToConfig("rootKey", _parserMock.Object);

                converting
                    .Enumerating()
                    .Should()
                    .Throw<Exception>()
                    .WithMessage(
                        "The key must not be null or empty. Ensure that there is at least one key under the root of the config or that the data there contains more than just a single value.");
            }
        }

        public sealed class HasValue : KVPairExtensionsTests
        {
            public static IEnumerable<object[]> TestCases => new List<object[]>
            {
                new object[]
                {
                    new KVPair("key") { Value = null },
                    false
                },
                new object[]
                {
                    new KVPair("key") { Value = new byte[0] },
                    false
                },
                new object[]
                {
                    new KVPair("key/")
                    {
                        Value = new byte[]
                        {
                            1
                        }
                    },
                    false
                },
                new object[]
                {
                    new KVPair("key")
                    {
                        Value = new byte[]
                        {
                            1
                        }
                    },
                    true
                }
            };

            [Theory]
            [MemberData(nameof(TestCases))]
            private void ShouldReturnTrueIfKVPairIsLeafWithNonEmptyArrayValue(KVPair kvPair, bool expected)
            {
                var hasValue = kvPair.HasValue();

                hasValue.Should().Be(expected);
            }
        }

        public sealed class IsLeafNode : KVPairExtensionsTests
        {
            [Theory]
            [InlineData("key", true)]
            [InlineData("key/", false)]
            private void ShouldBeTrueIfKeyDoesNotEndWithAForwardSlash(string key, bool expected)
            {
                var kvPair = new KVPair(key);

                var isLeafNode = kvPair.IsLeafNode();

                isLeafNode.Should().Be(expected);
            }
        }
    }
}