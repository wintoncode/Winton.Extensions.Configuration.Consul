using System;
using System.Threading;
using FluentAssertions;
using Winton.Extensions.Configuration.Consul.Parsers.Json;
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
                var source = new ConsulConfigurationSource("Key", default(CancellationToken));

                source.Parser.Should().BeOfType<JsonConfigurationParser>();
            }

            [Fact]
            private void ShouldSetCancellationTokenInConstructor()
            {
                var cancellationToken = new CancellationToken(false);
                var source = new ConsulConfigurationSource("Key", cancellationToken);

                source.CancellationToken.Should().Be(cancellationToken);
            }

            [Fact]
            private void ShouldSetKey()
            {
                var source = new ConsulConfigurationSource("Key", default(CancellationToken));

                source.Key.Should().Be("Key");
            }

            [Fact]
            private void ShouldSetOptionalToFalseByDefault()
            {
                var source = new ConsulConfigurationSource("Key", default(CancellationToken));

                source.Optional.Should().BeFalse();
            }

            [Fact]
            private void ShouldSetReloadOnChangeToFalseByDefault()
            {
                var source = new ConsulConfigurationSource("Key", default(CancellationToken));

                source.ReloadOnChange.Should().BeFalse();
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            private void ShouldThrowIfKeyIsInvalid(string key)
            {
                // ReSharper disable once ObjectCreationAsStatement
                Action constructing = () => new ConsulConfigurationSource(key, default(CancellationToken));

                constructing.ShouldThrow<ArgumentNullException>().And.Message.Should().Contain("key");
            }
        }
    }
}