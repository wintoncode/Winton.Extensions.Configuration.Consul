// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Winton.Extensions.Configuration.Consul.Parsers.Json
{
    internal sealed class JsonPrimitiveVisitor
    {
        private readonly Stack<string> _context = new Stack<string>();

        /// <summary>
        /// Recursively visits each primitive of the JSON object using depth-first traversal.
        /// </summary>
        /// <param name="jObject">The jObject to visit.</param>
        /// <returns>A KV pair for the full path to the property and its value</returns>
        public ICollection<KeyValuePair<string, string>> VisitJObject(JObject jObject)
        {
            return jObject.Properties().SelectMany(property => VisitProperty(property.Name, property.Value)).ToList();
        }

        private ICollection<KeyValuePair<string, string>> VisitArray(JArray array)
        {
            return array.SelectMany((token, index) => VisitProperty(index.ToString(), token)).ToList();
        }

        private ICollection<KeyValuePair<string, string>> VisitProperty(string key, JToken token)
        {
            _context.Push(key);
            var primitives = VisitToken(token);
            _context.Pop();
            return primitives.ToList();
        }

        private ICollection<KeyValuePair<string, string>> VisitToken(JToken token)
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

        private ICollection<KeyValuePair<string, string>> VisitPrimitive(JToken primitive)
        {
            if (_context.Count == 0)
            {
                throw new Exception("You're yielding so the context has changed by that time");
            }

            var key = ConfigurationPath.Combine(_context.Reverse());
            return new[] { new KeyValuePair<string, string>(key, primitive.Value<string>()) };
        }
    }
}