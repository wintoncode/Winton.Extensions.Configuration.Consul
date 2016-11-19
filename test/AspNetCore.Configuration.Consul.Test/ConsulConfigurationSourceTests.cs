using System;
using System.Threading;
using Chocolate.AspNetCore.Configuration.Consul.Parsers.Json;
using NUnit.Framework;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    [TestFixture]
    internal sealed class ConsulConfigurationSourceTests
    {
        [Test]
        public void ShouldSetKeyInConstructor()
        {
            const string key = "Key";
            var source = new ConsulConfigurationSource(key, new CancellationToken());

            Assert.That(source.Key, Is.EqualTo(key));
        }

        [Test]
        public void ShouldThrowIfKeyIsNullWhenConstructed()
        {
            Assert.That(
                () => new ConsulConfigurationSource(null, new CancellationToken()), 
                Throws.TypeOf<ArgumentNullException>()
                    .And.Message.Contains("key"));
        }

        [Test]
        public void ShouldThrowIfKeyIsWhitespaceWhenConstructed()
        {
            Assert.That(
                () => new ConsulConfigurationSource("   ", new CancellationToken()), 
                Throws.TypeOf<ArgumentNullException>()
                    .And.Message.Contains("key"));
        }

        [Test]
        public void ShouldSetCancellationTokensInConstructor()
        {
            var cancellationToken = new CancellationToken();
            var source = new ConsulConfigurationSource("Key", cancellationToken);

            Assert.That(source.CancellationToken, Is.EqualTo(cancellationToken));
        }

        [Test]
        public void ShouldHaveJsonConfgurationParserByDefault()
        {
            var source = new ConsulConfigurationSource("Key", new CancellationToken());

            Assert.That(source.Parser, Is.TypeOf<JsonConfigurationParser>());
        }

        [Test]
        public void ShouldSetOptionalToFalseByDefault()
        {
            var source = new ConsulConfigurationSource("Key", new CancellationToken());

            Assert.That(source.Optional, Is.False);
        }

        [Test]
        public void ShouldSetReloadOnChangeToFalseByDefault()
        {
            var source = new ConsulConfigurationSource("Key", new CancellationToken());

            Assert.That(source.ReloadOnChange, Is.False);
        }
    }
}