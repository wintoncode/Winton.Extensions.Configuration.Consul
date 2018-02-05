using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Winton.Extensions.Configuration.Consul.Parsers.Json
{
    public class JsonPrimitiveVisitorTests
    {
        private readonly JsonPrimitiveVisitor _visitor;

        public JsonPrimitiveVisitorTests()
        {
            _visitor = new JsonPrimitiveVisitor();
        }

        public sealed class VisitJObject : JsonPrimitiveVisitorTests
        {
            public static IEnumerable<object[]> TestCases => new List<object[]>
            {
                new object[]
                {
                    new JObject(new JProperty("Test", new JValue("string"))),
                    new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Test", "string")
                    }
                },
                new object[]
                {
                    new JObject(new JProperty("Test", new JValue(1))),
                    new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Test", "1")
                    }
                },
                new object[]
                {
                    new JObject(new JProperty("Test", new JValue(1.5))),
                    new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Test", "1.5")
                    }
                },
                new object[]
                {
                    new JObject(new JProperty("Test", new JValue(true))),
                    new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Test", "True")
                    }
                },
                new object[]
                {
                    new JObject(new JProperty("Parent", new JObject(new JProperty("Child", new JValue("primitive"))))),
                    new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Parent:Child", "primitive")
                    }
                },
                new object[]
                {
                    new JObject(new JProperty("Test", new JArray(new JValue("First"), new JValue("Second")))),
                    new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Test:0", "First"),
                        new KeyValuePair<string, string>("Test:1", "Second")
                    }
                },
                new object[]
                {
                    new JObject(new JProperty("Array", new JArray(new JObject(new JProperty("ObjectInArray", 1))))),
                    new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Array:0:ObjectInArray", "1")
                    }
                }
            };

            [Theory]
            [MemberData(nameof(TestCases))]
            private void ShouldVisitAllPrimitivesInJObject(
                JObject jObject,
                IEnumerable<KeyValuePair<string, string>> expected)
            {
                IEnumerable<KeyValuePair<string, string>> primitives = _visitor.VisitJObject(jObject);

                primitives.Should().BeEquivalentTo(expected);
            }
        }
    }
}