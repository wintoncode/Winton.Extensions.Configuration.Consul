using System.IO;
using System.Text;
using NUnit.Framework;

namespace Chocolate.AspNetCore.Configuration.Consul.Parsers.Json
{
    [TestFixture]
    [TestOf(nameof(JsonConfigurationParser))]
    internal sealed class JsonConfigurationParserTests
    {
        private JsonConfigurationParser _parser;

        [SetUp]
        public void SetUp()
        {
            _parser = new JsonConfigurationParser();
        }

        [Test]
        public void ShouldParseSimpleJsonFromStream()
        {
            const string key = "Key";
            const string value = "Value"; 
            string json = $"{{\"{key}\": \"{value}\"}}";
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = _parser.Parse(stream);
                Assert.That(result[key], Is.EqualTo(value));
            }
        }

        [Test]
        public void ShouldParseComplexJsonFromStream()
        {
            const string parentKey = "parentKey";
            const string childKey = "childKey";
            const string value = "Value"; 
            string json = $"{{\"{parentKey}\": {{\"{childKey}\": \"{value}\"}} }}";
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = _parser.Parse(stream);
                Assert.That(result[$"{parentKey}:{childKey}"], Is.EqualTo(value));
            }
        }
    }
}