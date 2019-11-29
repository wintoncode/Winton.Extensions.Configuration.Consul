using System;
using System.Threading;
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
            private void ShouldHaveJsonConfgurationParserByDefault()
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
        }
    }
}