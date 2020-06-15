using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Consul;
using FluentAssertions;
using Winton.Extensions.Configuration.Consul.Parsers;
using Xunit;

namespace Winton.Extensions.Configuration.Consul
{
    public class ConsulConfigurationSourceTests
    {
        public sealed class Constructor : ConsulConfigurationSourceTests
        {
            [Fact]
            private void ShouldHaveJsonConfigurationParserByDefault()
            {
                var source = new ConsulConfigurationSource("Key");

                source.Parser.Should().BeOfType<JsonConfigurationParser>();
            }

            [Fact]
            private void ShouldSetKey()
            {
                var source = new ConsulConfigurationSource("Key");

                source.Key.Should().Be("Key");
            }

            [Fact]
            private void ShouldSetKeyToRemoveToKeyByDefault()
            {
                var source = new ConsulConfigurationSource("Key");

                source.KeyToRemove.Should().Be("Key");
            }

            [Fact]
            private void ShouldSetOptionalToFalseByDefault()
            {
                var source = new ConsulConfigurationSource("Key");

                source.Optional.Should().BeFalse();
            }

            [Fact]
            private void ShouldSetReloadOnChangeToFalseByDefault()
            {
                var source = new ConsulConfigurationSource("Key");

                source.ReloadOnChange.Should().BeFalse();
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            private void ShouldThrowIfKeyIsInvalid(string key)
            {
                // ReSharper disable once ObjectCreationAsStatement
                Action constructing = () => new ConsulConfigurationSource(key);

                constructing.Should().Throw<ArgumentNullException>().And.Message.Should().Contain("key");
            }

            [Fact]
            private void ShoulSetDefaultConvertConsulKVPairToConfigStrategy()
            {
                var source = new ConsulConfigurationSource("Key");

                var consulKVPair = new KVPair("key") { Value = Encoding.UTF8.GetBytes("{\"a\": \"b\", \"c\": \"d\"}") };

                var result = source.ConvertConsulKVPairToConfig(consulKVPair);
                result.Should()
                    .NotBeEmpty()
                    .And.HaveCount(2)
                    .And.Contain(kvp => kvp.Key == "key:a" && kvp.Value == "b")
                    .And.Contain(kvp => kvp.Key == "key:c" && kvp.Value == "d");
            }
        }
    }
}