using System;
using System.Threading;
using NUnit.Framework;
using Winton.Extensions.Configuration.Consul.Parsers.Json;

namespace Winton.Extensions.Configuration.Consul
{
    [TestFixture]
    internal sealed class ConsulConfigurationSourceTests
    {
        [Test]
        public void ShouldSetKeyInConstructor()
        {
            const string key = "Key";
            var source = new ConsulConfigurationSource(key, CancellationToken.None);

            Assert.That(source.Key, Is.EqualTo(key));
        }

        [Test]
        public void ShouldThrowIfKeyIsNullWhenConstructed()
        {
            Assert.That(
                () => new ConsulConfigurationSource(null, CancellationToken.None),
                Throws.TypeOf<ArgumentNullException>()
                    .And.Message.Contains("key"));
        }

        [Test]
        public void ShouldThrowIfKeyIsWhitespaceWhenConstructed()
        {
            Assert.That(
                () => new ConsulConfigurationSource("   ", CancellationToken.None),
                Throws.TypeOf<ArgumentNullException>()
                    .And.Message.Contains("key"));
        }

        [Test]
        public void ShouldSetCancellationTokensInConstructor()
        {
            var cancellationToken = CancellationToken.None;
            var source = new ConsulConfigurationSource("Key", cancellationToken);

            Assert.That(source.CancellationToken, Is.EqualTo(cancellationToken));
        }

        [Test]
        public void ShouldHaveJsonConfgurationParserByDefault()
        {
            var source = new ConsulConfigurationSource("Key", CancellationToken.None);

            Assert.That(source.Parser, Is.TypeOf<JsonConfigurationParser>());
        }

        [Test]
        public void ShouldSetOptionalToFalseByDefault()
        {
            var source = new ConsulConfigurationSource("Key", CancellationToken.None);

            Assert.That(source.Optional, Is.False);
        }

        [Test]
        public void ShouldSetReloadOnChangeToFalseByDefault()
        {
            var source = new ConsulConfigurationSource("Key", CancellationToken.None);

            Assert.That(source.ReloadOnChange, Is.False);
        }
    }
}