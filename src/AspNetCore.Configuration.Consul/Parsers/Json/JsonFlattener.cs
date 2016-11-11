using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chocolate.AspNetCore.Configuration.Consul.Parsers.Json
{
    internal sealed class JsonFlattener
    {
        private readonly JsonReader _jsonReader;

        public JsonFlattener(JsonReader jsonReader)
        {
            _jsonReader = jsonReader;
            _jsonReader.DateParseHandling = DateParseHandling.None;
        }

        public IDictionary<string, string> Flatten()
        {
            JObject jsonConfig = JObject.Load(_jsonReader);
            var jsonPrimitiveVisitor = new JsonPrimitiveVisitor();
            IDictionary<string, string> data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> primitive in jsonPrimitiveVisitor.VisitJObject(jsonConfig))
            {
                if (data.ContainsKey(primitive.Key))
                {
                    throw new FormatException($"Key {primitive.Key} is duplicated in json");
                }
                data.Add(primitive);
            }
            return data;
        }
    }
}