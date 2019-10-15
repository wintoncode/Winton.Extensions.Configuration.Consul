using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Winton.Extensions.Configuration.Consul.Parsers
{
    public class SimpleConfigurationParserTests
    {
        private readonly SimpleConfigurationParser _parser;

        public SimpleConfigurationParserTests()
        {
            _parser = new SimpleConfigurationParser();
        }

        public sealed class Parse : SimpleConfigurationParserTests
        {
            [Fact]
            private void ShouldParseSimpleValueFromStream()
            {
                using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("value"));
                var result = _parser.Parse(stream);

                result.Should().BeEquivalentTo(new Dictionary<string, string> { { string.Empty, "value" } });
            }
        }
    }
}