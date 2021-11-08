// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor.Serialization;

internal static class JsonReaderExtensions
{
    public static bool ReadTokenAndAdvance(this JsonReader reader, JsonToken expectedTokenType, out object value)
    {
        value = reader.Value;
        return reader.TokenType == expectedTokenType && reader.Read();
    }

    public static void ReadProperties(this JsonReader reader, Action<string> onProperty)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    var propertyName = reader.Value.ToString();
                    onProperty(propertyName);
                    break;
                case JsonToken.EndObject:
                    return;
            }
        }
    }

    public static string ReadNextStringProperty(this JsonReader reader, string propertyName)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    Debug.Assert(reader.Value.ToString() == propertyName);
                    if (reader.Read())
                    {
                        var value = (string)reader.Value;
                        return value;
                    }
                    else
                    {
                        return null;
                    }
            }
        }

        throw new JsonSerializationException($"Could not find string property '{propertyName}'.");
    }
}
