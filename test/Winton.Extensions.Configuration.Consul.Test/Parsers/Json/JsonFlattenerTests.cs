using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Winton.Extensions.Configuration.Consul.Parsers.Json
{
    [TestFixture]
    internal sealed class JsonFlattenerTests
    {
        [Test]
        public void ShouldFlattenJObjectToCaseInsensitiveDictionary()
        {
            const string key = "Key";
            const string value = "Value";
            var jObject = new JObject(new JProperty(key, new JValue(value)));

            IDictionary<string, string> flattenedObject = jObject.Flatten();

            Assert.That(flattenedObject.ContainsKey(key.ToUpper()));
            Assert.That(flattenedObject.ContainsKey(key.ToLower()));
            Assert.That(flattenedObject[key], Is.EqualTo(value));
        }

        [Test]
        public void ShouldThrowIfDuplicateKeyWhenFlattened()
        {
            const string key = "Key";
            const string value = "Value";
            var jObject = new JObject(
                new JProperty(key.ToUpper(), new JValue(value)),
                new JProperty(key.ToLower(), new JValue(value)));

            Assert.That(() => jObject.Flatten(), Throws.TypeOf<FormatException>());
        }
    }
}