// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.HttpRepl.Preferences;
using Microsoft.Repl.ConsoleHandling;
using Newtonsoft.Json;

namespace Microsoft.HttpRepl.Formatting
{
    public static class JsonVisitor
    {
        public static string FormatAndColorize(IJsonConfig config, string jsonData)
        {
            if (jsonData == null)
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder();
            JsonTextReader reader = new JsonTextReader(new StringReader(jsonData));
            bool isValuePosition = false;
            bool isTerminalValue = false;
            bool isFirstToken = true;

            while (reader.Read())
            {
                if (!isValuePosition)
                {
                    //If we're about to write an end object/array, we shouldn't have a comma
                    if (reader.TokenType != JsonToken.EndArray && reader.TokenType != JsonToken.EndObject
                        && isTerminalValue)
                    {
                        result.Append(",".SetColor(config.CommaColor));
                    }

                    if (!isFirstToken)
                    {
                        result.AppendLine();
                    }
                }

                isFirstToken = false;

                if (!isValuePosition)
                {
                    result.Append("".PadLeft(reader.Depth * config.IndentSize));
                }

                isTerminalValue = false;
                isValuePosition = false;
                JsonToken type = reader.TokenType;

                switch (type)
                {
                    case JsonToken.StartObject:
                        result.Append("{".SetColor(config.ObjectBraceColor));
                        break;
                    case JsonToken.EndObject:
                        result.Append("}".SetColor(config.ObjectBraceColor));
                        isTerminalValue = true;
                        break;
                    case JsonToken.StartArray:
                        result.Append("[".SetColor(config.ArrayBraceColor));
                        break;
                    case JsonToken.EndArray:
                        result.Append("]".SetColor(config.ArrayBraceColor));
                        isTerminalValue = true;
                        break;
                    case JsonToken.PropertyName:
                        result.Append((reader.QuoteChar.ToString() + reader.Value + reader.QuoteChar).SetColor(config.NameColor) + ": ".SetColor(config.NameSeparatorColor));
                        isValuePosition = true;
                        break;
                    case JsonToken.Boolean:
                        result.Append(reader.Value.ToString().ToLowerInvariant().SetColor(config.BoolColor));
                        isTerminalValue = true;
                        break;
                    case JsonToken.Integer:
                    case JsonToken.Float:
                        result.Append(reader.Value.ToString().ToLowerInvariant().SetColor(config.NumericColor));
                        isTerminalValue = true;
                        break;
                    case JsonToken.Null:
                        result.Append("null".SetColor(config.NullColor));
                        isTerminalValue = true;
                        break;
                    case JsonToken.Comment:
                        result.Append(("//" + reader.Value).SetColor(config.NumericColor));
                        break;
                    case JsonToken.String:
                        result.Append((reader.QuoteChar.ToString() + reader.Value + reader.QuoteChar.ToString()).SetColor(config.StringColor));
                        isTerminalValue = true;
                        break;
                    case JsonToken.Raw:
                    case JsonToken.Date:
                    case JsonToken.Bytes:
                    case JsonToken.Undefined:
                    case JsonToken.None:
                        result.Append(reader.Value.ToString().SetColor(config.DefaultColor));
                        isTerminalValue = true;
                        break;
                    case JsonToken.EndConstructor:
                    case JsonToken.StartConstructor:
                    default:
                        result.Append(reader.Value.ToString().SetColor(config.DefaultColor));
                        break;
                }
            }

            return result.ToString();
        }
    }
}
