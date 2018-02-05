using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Winton.Extensions.Configuration.Consul.Parsers.Json
{
    public class JsonConfigurationParserTests
    {
        private readonly JsonConfigurationParser _parser;

        public JsonConfigurationParserTests()
        {
            _parser = new JsonConfigurationParser();
        }

        public sealed class Parse : JsonConfigurationParserTests
        {
            [Theory]
            [InlineData("{\"Key\": \"Value\"}", "Key", "Value")]
            [InlineData("{\"parent\": {\"child\": \"Value\"} }", "parent:child", "Value")]
            public void ShouldParseSimpleJsonFromStream(string json, string key, string expectedValue)
            {
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    IDictionary<string, string> result = _parser.Parse(stream);
                    result[key].Should().Be(expectedValue);
                }
            }
        }
    }
}