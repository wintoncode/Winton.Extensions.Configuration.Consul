using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Winton.Extensions.Configuration.Consul.Parsers.Json
{
    public class JsonFlattenerTests
    {
        public sealed class Flatten : JsonFlattenerTests
        {
            [Theory]
            [InlineData("Key")]
            [InlineData("key")]
            [InlineData("KEY")]
            private void ShouldFlattenJObjectToCaseInsensitiveDictionary(string lookupKey)
            {
                var jObject = new JObject(new JProperty("Key", new JValue("Value")));

                IDictionary<string, string> flattenedObject = jObject.Flatten();

                flattenedObject[lookupKey].Should().Be("Value");
            }

            [Theory]
            [InlineData("Key", "key")]
            [InlineData("Key", "KEY")]
            private void ShouldThrowIfDuplicateKeyWhenFlattened(string key1, string key2)
            {
                var jObject = new JObject(
                    new JProperty(key1, new JValue("Value")),
                    new JProperty(key2, new JValue("Value")));

                Action flattening = () => jObject.Flatten();

                flattening.Should().Throw<FormatException>();
            }
        }
    }
}