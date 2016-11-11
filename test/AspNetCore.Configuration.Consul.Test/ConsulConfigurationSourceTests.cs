using Chocolate.AspNetCore.Configuration.Consul.Parsers.Json;
using NUnit.Framework;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    [TestFixture]
    internal sealed class ConsulConfigurationSourceTests
    {
        [Test]
        public void ShouldHaveJsonConfgurationParserByDefault()
        {
            var source = new ConsulConfigurationSource();

            Assert.That(source.Parser, Is.TypeOf<JsonConfigurationParser>());
        }

        [Test]
        public void ShouldSetOptionalToFalseByDefault()
        {
            var source = new ConsulConfigurationSource();

            Assert.That(source.Optional, Is.False);
        }

        [Test]
        public void ShouldSetReloadOnChangeToFalseByDefault()
        {
            var source = new ConsulConfigurationSource();

            Assert.That(source.ReloadOnChange, Is.False);
        }
    }
}