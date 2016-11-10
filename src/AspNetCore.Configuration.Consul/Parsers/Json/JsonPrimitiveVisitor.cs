using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace Chocolate.AspNetCore.Configuration.Consul.Parsers.Json
{
    internal sealed class JsonPrimitiveVisitor
    {
        private readonly Stack<string> _context = new Stack<string>();
        private readonly JObject _jObject;

        public JsonPrimitiveVisitor(JObject jObject)
        {
            _jObject = jObject;
        }

        /// <summary>
        /// Recursively visits each primitive of the JSON object using depth-first traversal.
        /// </summary>
        /// <returns>A KV pair for the full path to the property and its value</returns>
        public IEnumerable<KeyValuePair<string, string>> VisitJObject(JObject jObject)
        {
            return jObject.Properties().SelectMany(property => VisitProperty(property.Name, property.Value));
        }

        private IEnumerable<KeyValuePair<string, string>> VisitArray(JArray array)
        {
            return array.SelectMany((token, index) => VisitProperty(index.ToString(), token));
        }

        private IEnumerable<KeyValuePair<string, string>> VisitProperty(string key, JToken value)
        {
            _context.Push(key);
            var primitives = VisitToken(value);
            _context.Pop();
            return primitives;
        }

        private IEnumerable<KeyValuePair<string, string>> VisitToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return VisitJObject(token.Value<JObject>());
                case JTokenType.Array:
                    return VisitArray(token.Value<JArray>());
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Bytes:
                case JTokenType.Raw:
                case JTokenType.Null:
                    return VisitPrimitive(token);
                default:
                    throw new FormatException($"Error parsing JSON. {token.Type} is not a supported token.");
            }
        }

        private IEnumerable<KeyValuePair<string, string>> VisitPrimitive(JToken primitive)
        {
            var key = ConfigurationPath.Combine(_context.Reverse());
            yield return new KeyValuePair<string, string>(key, primitive.ToString());
        }
    }
}