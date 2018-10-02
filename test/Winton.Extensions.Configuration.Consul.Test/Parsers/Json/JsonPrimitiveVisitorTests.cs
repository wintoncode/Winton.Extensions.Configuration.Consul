using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Winton.Extensions.Configuration.Consul.Parsers.Json
{
    [TestFixture]
    internal sealed class JsonPrimitiveVisitorTests
    {
        [Test]
        [TestCase("string", "string")]
        [TestCase(1, "1")]
        [TestCase(1.5, "1.5")]
        [TestCase(true, "True")]
        public void ShouldVisitPrimitivesWhenSimpleObject(object value, string expected)
        {
            const string key = "Test";
            var jObject = new JObject { new JProperty(key, new JValue(value)) };

            var visitor = new JsonPrimitiveVisitor();

            var expectedPrimitive = new KeyValuePair<string, string>(key, expected);
            ICollection<KeyValuePair<string, string>> expectedPrimitives = new[] { expectedPrimitive };
            Assert.That(visitor.VisitJObject(jObject).ToList(), Is.EqualTo(expectedPrimitives).AsCollection);
        }

        [Test]
        public void ShouldConvertPrimitivesToStringUsingJsonSerializerCulture()
        {
            CultureInfo originalCuluture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            try
            {
                const string key = "Test";
                var jObject = new JObject { new JProperty(key, new JValue(1.5)) };

                var visitor = new JsonPrimitiveVisitor();

                var expectedPrimitive = new KeyValuePair<string, string>(key, "1.5");
                ICollection<KeyValuePair<string, string>> expectedPrimitives = new[] { expectedPrimitive };
                Assert.That(visitor.VisitJObject(jObject).ToList(), Is.EqualTo(expectedPrimitives).AsCollection);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCuluture;
            }
        }

        [Test]
        public void ShouldVisitPrimitivesWhenObjectContainsChildObject()
        {
            const string parentKey = "Parent";
            const string childKey = "Child";
            const string value = "primitive";
            var jObject = new JObject(
                new JProperty(
                    parentKey,
                    new JObject(new JProperty(childKey, new JValue(value)))));

            var visitor = new JsonPrimitiveVisitor();

            var expectedPrimitive = new KeyValuePair<string, string>(parentKey, value);
            ICollection<KeyValuePair<string, string>> expectedPrimitives = new[]
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
                new JProperty(
                    key,
                    new JArray(
                        new JValue(firstValue),
                        new JValue(secondValue))));

            var visitor = new JsonPrimitiveVisitor();

            ICollection<KeyValuePair<string, string>> expectedPrimitives = new[]
                {
                    new KeyValuePair<string, string>($"{key}:0", firstValue),
                    new KeyValuePair<string, string>($"{key}:1", secondValue)
                };
            Assert.That(visitor.VisitJObject(jObject).ToList(), Is.EqualTo(expectedPrimitives).AsCollection);
        }

        [Test]
        public void ShouldVisitPrimitivesForObjectsInAnArray()
        {
            const string key = "Array";
            const string nestedObjectKey = "ObjectInArray";
            const int value = 1;
            var jObject = new JObject(
                new JProperty(
                    key,
                    new JArray(
                        new JObject(
                            new JProperty(nestedObjectKey, value)))));

            var visitor = new JsonPrimitiveVisitor();

            ICollection<KeyValuePair<string, string>> expectedPrimitives = new[]
                {
                    new KeyValuePair<string, string>($"{key}:0:{nestedObjectKey}", value.ToString()),
                };
            Assert.That(visitor.VisitJObject(jObject).ToList(), Is.EqualTo(expectedPrimitives).AsCollection);
        }
    }
}