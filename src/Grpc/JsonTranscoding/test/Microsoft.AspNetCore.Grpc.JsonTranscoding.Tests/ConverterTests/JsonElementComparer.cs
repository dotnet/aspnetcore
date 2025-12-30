// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.ConverterTests;

public class JsonElementComparer : IEqualityComparer<JsonElement>
{
    public JsonElementComparer() : this(maxHashDepth: -1, compareRawStrings: false) { }

    public JsonElementComparer(int maxHashDepth, bool compareRawStrings)
    {
        MaxHashDepth = maxHashDepth;
        CompareRawStrings = compareRawStrings;
    }

    private int MaxHashDepth { get; }
    private bool CompareRawStrings { get; }

    #region IEqualityComparer<JsonElement> Members

    public bool Equals(JsonElement x, JsonElement y)
    {
        if (x.ValueKind != y.ValueKind)
        {
            return false;
        }

        switch (x.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Undefined:
                return true;

            // Compare the raw values of numbers, and the text of strings.
            // Note this means that 0.0 will differ from 0.00 -- which may be correct as deserializing either to `decimal` will result in subtly different results.
            // Newtonsoft's JValue.Compare(JTokenType valueType, object? objA, object? objB) has logic for detecting "equivalent" values,
            // you may want to examine it to see if anything there is required here.
            // https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/Linq/JValue.cs#L246
            case JsonValueKind.Number:
                return x.GetRawText() == y.GetRawText();

            case JsonValueKind.String:
                if (CompareRawStrings)
                {
                    return x.GetRawText() == y.GetRawText();
                }
                else
                {
                    // Automatically resolve JSON escape sequences to their corresponding characters.
                    return x.GetString() == y.GetString();
                }

            case JsonValueKind.Array:
                return x.EnumerateArray().SequenceEqual(y.EnumerateArray(), this);

            case JsonValueKind.Object:
                {
                    // Surprisingly, JsonDocument fully supports duplicate property names.
                    // I.e. it's perfectly happy to parse {"Value":"a", "Value" : "b"} and will store both
                    // key/value pairs inside the document!
                    // A close reading of https://www.rfc-editor.org/rfc/rfc8259#section-4 seems to indicate that
                    // such objects are allowed but not recommended, and when they arise, interpretation of
                    // identically-named properties is order-dependent.
                    // So stably sorting by name then comparing values seems the way to go.
                    var xPropertiesUnsorted = x.EnumerateObject().ToList();
                    var yPropertiesUnsorted = y.EnumerateObject().ToList();
                    if (xPropertiesUnsorted.Count != yPropertiesUnsorted.Count)
                    {
                        return false;
                    }

                    var xProperties = xPropertiesUnsorted.OrderBy(p => p.Name, StringComparer.Ordinal);
                    var yProperties = yPropertiesUnsorted.OrderBy(p => p.Name, StringComparer.Ordinal);
                    foreach (var (px, py) in xProperties.Zip(yProperties))
                    {
                        if (px.Name != py.Name)
                        {
                            return false;
                        }

                        if (!Equals(px.Value, py.Value))
                        {
                            return false;
                        }
                    }
                    return true;
                }

            default:
                throw new JsonException(string.Format(CultureInfo.InvariantCulture, "Unknown JsonValueKind {0}", x.ValueKind));
        }
    }

    public int GetHashCode(JsonElement obj)
    {
        var hash = new HashCode(); // New in .Net core: https://learn.microsoft.com/dotnet/api/system.hashcode
        ComputeHashCode(obj, ref hash, 0);
        return hash.ToHashCode();
    }

    void ComputeHashCode(JsonElement obj, ref HashCode hash, int depth)
    {
        hash.Add(obj.ValueKind);

        switch (obj.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Undefined:
                break;

            case JsonValueKind.Number:
                hash.Add(obj.GetRawText());
                break;

            case JsonValueKind.String:
                hash.Add(obj.GetString());
                break;

            case JsonValueKind.Array:
                if (depth != MaxHashDepth)
                {
                    foreach (var item in obj.EnumerateArray())
                    {
                        ComputeHashCode(item, ref hash, depth + 1);
                    }
                }
                else
                {
                    hash.Add(obj.GetArrayLength());
                }

                break;

            case JsonValueKind.Object:
                foreach (var property in obj.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    hash.Add(property.Name);
                    if (depth != MaxHashDepth)
                    {
                        ComputeHashCode(property.Value, ref hash, depth + 1);
                    }
                }
                break;

            default:
                throw new JsonException(string.Format(CultureInfo.InvariantCulture, "Unknown JsonValueKind {0}", obj.ValueKind));
        }
    }

    #endregion
}
