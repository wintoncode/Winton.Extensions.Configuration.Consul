// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Winton.Extensions.Configuration.Consul.Internals
{
    internal class JsonConfigurationFileParser
    {
        private readonly IDictionary<string, string> _data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _context = new Stack<string>();
        private string? _currentPath;
        private string? _dataPrefix;

        private JsonConfigurationFileParser(string? prefix = null)
        {
            _dataPrefix = prefix;
        }

        public static IDictionary<string, string> Parse(Stream input)
            => new JsonConfigurationFileParser().ParseStream(input);

        private IDictionary<string, string> ParseStream(Stream input)
        {
            _data.Clear();

            var jsonDocumentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            using (var reader = new StreamReader(input))
            using (JsonDocument doc = JsonDocument.Parse(reader.ReadToEnd(), jsonDocumentOptions))
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object && doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new FormatException(/*Resources.FormatError_UnsupportedJSONToken(doc.RootElement.ValueKind)*/);
                }

                switch (doc.RootElement.ValueKind)
                {
                    case JsonValueKind.Object:
                        VisitElement(doc.RootElement);
                        break;
                    case JsonValueKind.Array:
                        VisitArrayElement(doc.RootElement);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return _data;
        }

        private void VisitElement(JsonElement element)
        {
            foreach (var property in element.EnumerateObject())
            {
                EnterContext(property.Name);
                VisitValue(property.Value);
                ExitContext();
            }
        }

        private void VisitArrayElement(JsonElement element)
        {
            var prefixIdx = 0;
            foreach (var arrayElement in element.EnumerateArray())
            {
                switch (arrayElement.ValueKind)
                {
                    case JsonValueKind.Number:
                    case JsonValueKind.String:
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                    case JsonValueKind.Null:
                        EnterContext($"{(_dataPrefix is null ? $"{prefixIdx}" : $"{_dataPrefix}:{prefixIdx}")}");
                        VisitValue(arrayElement);
                        ExitContext();
                        break;
                    case JsonValueKind.Object:
                        foreach (var property in arrayElement.EnumerateObject())
                        {
                            EnterContext($"{(_dataPrefix is null ? $"{prefixIdx}:" : $"{_dataPrefix}:{prefixIdx}:")}{property.Name}");
                            VisitValue(property.Value);
                            ExitContext();
                        }

                        break;
                    default:
                        throw new NotSupportedException();
                }

                prefixIdx++;
            }
        }

        private void VisitValue(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    VisitElement(value);
                    break;

                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var arrayElement in value.EnumerateArray())
                    {
                        EnterContext(index.ToString());
                        VisitValue(arrayElement);
                        ExitContext();
                        index++;
                    }

                    break;

                case JsonValueKind.Number:
                case JsonValueKind.String:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    var key = _currentPath;
                    if (_data.ContainsKey(key ?? string.Empty))
                    {
                        throw new FormatException(/*Resources.FormatError_KeyIsDuplicated(key)*/);
                    }

                    _data[key ?? string.Empty] = value.ToString();
                    break;

                default:
                    throw new FormatException(/*Resources.FormatError_UnsupportedJSONToken(value.ValueKind)*/);
            }
        }

        private void EnterContext(string context)
        {
            _context.Push(context);
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private void ExitContext()
        {
            _context.Pop();
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }
    }
}