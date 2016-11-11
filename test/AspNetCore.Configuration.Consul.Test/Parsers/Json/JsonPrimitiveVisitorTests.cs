using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Chocolate.AspNetCore.Configuration.Consul.Parsers.Json
{
    [TestFixture]
    internal sealed class JsonPrimitiveVisitorTests
    {
        private JsonPrimitiveVisitor _jsonPrimitiveVisitor;

        [Test]
        [TestCase("string")]
        [TestCase(1)]
        [TestCase(1.5)]
        [TestCase(true)]
        public void ShouldVisitPrimitivesWhenSimpleObject(object value)
        {
            const string key = "Test";
            var property = new JProperty(key, new JValue(value));
            var jObject = new JObject();
            jObject.Add(property);

            var visitor = new JsonPrimitiveVisitor();

            var expectedPrimitive = new KeyValuePair<string, string>(key, value.ToString());
            ICollection<KeyValuePair<string, string>> expectedPrimitives = new [] {expectedPrimitive};
            Assert.That(visitor.VisitJObject(jObject).ToList(), Is.EqualTo(expectedPrimitives).AsCollection);
        }

        [Test]
        public void ShouldVisitPrimitivesWhenObjectContainsChildObject()
        {
            const string parentKey = "Parent";
            const string childKey = "Child";
            const string value = "primitive";
            var jObject = new JObject(
                new JProperty(parentKey,
                    new JObject(
                        new JProperty(childKey, new JValue(value))
                    )
                )
            );

            var visitor = new JsonPrimitiveVisitor();

            var expectedPrimitive = new KeyValuePair<string, string>(parentKey, value.ToString());
            ICollection<KeyValuePair<string, string>> expectedPrimitives = new []
                {
                    new KeyValuePair<string, string>($"{parentKey}:{childKey}", value)
                };
            Assert.That(visitor.VisitJObject(jObject).ToList(), Is.EqualTo(expectedPrimitives).AsCollection);
        }

        [Test]
        public void ShouldVisitPrimitivesWhenObjectContainsArray()
        {
            const string key = "Test";
            const string firstValue = "First";
            const string secondValue = "Second";
            var jObject = new JObject(
                new JProperty(key,
                    new JArray(
                        new JValue(firstValue),
                        new JValue(secondValue)
                    )
                )
            );

            var visitor = new JsonPrimitiveVisitor();

            ICollection<KeyValuePair<string, string>> expectedPrimitives = new []
                {
                    new KeyValuePair<string, string>($"{key}:0", firstValue),
                    new KeyValuePair<string, string>($"{key}:1", secondValue)
                };
            Assert.That(visitor.VisitJObject(jObject).ToList(), Is.EqualTo(expectedPrimitives).AsCollection);
        }
    }
}