// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Winton.Extensions.Configuration.Consul.Parsers.Json
{
    internal static class JsonFlattener
    {
        internal static IDictionary<string, string> Flatten(this JObject jObject)
        {
            var jsonPrimitiveVisitor = new JsonPrimitiveVisitor();
            IDictionary<string, string> data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> primitive in jsonPrimitiveVisitor.VisitJObject(jObject))
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