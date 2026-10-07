// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040 // The framework implements this experimental contract.

using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiValidatedJsonSchemaNormalizer
{
    private const string CanonicalDialectUri = "https://json-schema.org/draft/2020-12/schema";

    private static readonly string[] s_schemaKeywords =
    [
        "additionalItems",
        "additionalProperties",
        "contains",
        "contentSchema",
        "else",
        "if",
        "items",
        "not",
        "propertyNames",
        "then",
        "unevaluatedItems",
        "unevaluatedProperties",
    ];

    private static readonly string[] s_schemaArrayKeywords =
    [
        "allOf",
        "anyOf",
        "oneOf",
        "prefixItems",
    ];

    private static readonly string[] s_schemaMapKeywords =
    [
        "$defs",
        "definitions",
        "dependentSchemas",
        "patternProperties",
        "properties",
    ];

    public static byte[] Normalize(JsonElement schema, OpenApiJsonSchemaDialect dialect)
    {
        var dialectUri = GetDialectUri(dialect);
        ValidateDialect(schema, dialect, dialectUri);
        ValidateSchemaReferences(schema, schema, dialect);

        var node = JsonNode.Parse(schema.GetRawText()) ??
            throw new ArgumentException(Resources.ValidatedJsonSchemaMustBeSchema, nameof(schema));
        var normalized = NormalizeSchema(node, dialect, isRoot: true);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            normalized.WriteTo(writer);
        }
        return buffer.WrittenSpan.ToArray();
    }

    internal static string GetDialectUri(OpenApiJsonSchemaDialect dialect)
        => dialect switch
        {
            OpenApiJsonSchemaDialect.Draft4 => "http://json-schema.org/draft-04/schema#",
            OpenApiJsonSchemaDialect.Draft6 => "http://json-schema.org/draft-06/schema#",
            OpenApiJsonSchemaDialect.Draft7 => "http://json-schema.org/draft-07/schema#",
            OpenApiJsonSchemaDialect.Draft201909 => "https://json-schema.org/draft/2019-09/schema",
            OpenApiJsonSchemaDialect.Draft202012 => CanonicalDialectUri,
            _ => throw new ArgumentOutOfRangeException(nameof(dialect)),
        };

    private static void ValidateDialect(
        JsonElement schema,
        OpenApiJsonSchemaDialect dialect,
        string dialectUri)
    {
        if (schema.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            if (dialect == OpenApiJsonSchemaDialect.Draft4)
            {
                throw KeywordNotSupported("boolean schema", dialect);
            }
            return;
        }

        if (!schema.TryGetProperty("$schema", out var declaredDialect) ||
            declaredDialect.ValueKind != JsonValueKind.String ||
            !string.Equals(declaredDialect.GetString(), dialectUri, StringComparison.Ordinal))
        {
            throw new ArgumentException(Resources.FormatValidatedJsonSchemaDialectRequired(dialectUri), nameof(schema));
        }
    }

    private static JsonNode NormalizeSchema(
        JsonNode node,
        OpenApiJsonSchemaDialect dialect,
        bool isRoot = false)
    {
        if (node is JsonValue)
        {
            if (dialect == OpenApiJsonSchemaDialect.Draft4)
            {
                throw KeywordNotSupported("boolean schema", dialect);
            }
            return node.DeepClone();
        }

        if (node is not JsonObject source)
        {
            throw new ArgumentException(Resources.FormatValidatedJsonSchemaKeywordInvalid("schema", dialect));
        }

        ValidateDialectKeywords(source, dialect);
        ValidateKeywordShapes(source, dialect);

        if (dialect is (OpenApiJsonSchemaDialect.Draft4 or
            OpenApiJsonSchemaDialect.Draft6 or
            OpenApiJsonSchemaDialect.Draft7) &&
            source.ContainsKey("$ref"))
        {
            var referenceComposition = new JsonArray();
            referenceComposition.Add((JsonNode)new JsonObject
            {
                ["$ref"] = RewriteReference(source["$ref"]!.GetValue<string>(), dialect),
            });
            var referenceOnly = new JsonObject
            {
                ["allOf"] = referenceComposition,
            };
            if (source.TryGetPropertyValue("definitions", out var referenceDefinitions))
            {
                referenceOnly["$defs"] = NormalizeSchemaMap(referenceDefinitions!, dialect);
            }
            if (source.TryGetPropertyValue(
                    dialect == OpenApiJsonSchemaDialect.Draft4 ? "id" : "$id",
                    out var referenceId))
            {
                referenceOnly["$id"] = referenceId!.DeepClone();
            }
            if (isRoot)
            {
                referenceOnly["$schema"] = CanonicalDialectUri;
            }
            return referenceOnly;
        }

        var result = new JsonObject();
        foreach (var (name, value) in source)
        {
            if (name is "id" or "definitions" or "dependencies" or
                "additionalItems" or "$vocabulary")
            {
                continue;
            }
            if (dialect == OpenApiJsonSchemaDialect.Draft4 &&
                name is "exclusiveMinimum" or "exclusiveMaximum")
            {
                continue;
            }

            var normalizedName = name == "$schema" ? "$schema" : name;
            result[normalizedName] = NormalizeKeywordValue(name, value, dialect);
        }

        if (isRoot)
        {
            result["$schema"] = CanonicalDialectUri;
        }

        if (dialect == OpenApiJsonSchemaDialect.Draft4 &&
            source.TryGetPropertyValue("id", out var id))
        {
            result["$id"] = id!.DeepClone();
        }

        if (IsDefinitionsDialect(dialect) &&
            source.TryGetPropertyValue("definitions", out var definitions))
        {
            result["$defs"] = NormalizeSchemaMap(definitions!, dialect);
        }

        NormalizeExclusiveBound(source, result, dialect, "minimum", "exclusiveMinimum");
        NormalizeExclusiveBound(source, result, dialect, "maximum", "exclusiveMaximum");
        NormalizeTuple(source, result, dialect);
        NormalizeDependencies(source, result, dialect);
        return result;
    }

    private static JsonNode? NormalizeKeywordValue(
        string name,
        JsonNode? value,
        OpenApiJsonSchemaDialect dialect)
    {
        if (value is null)
        {
            return null;
        }

        if (name == "$ref")
        {
            return JsonValue.Create(RewriteReference(value.GetValue<string>(), dialect));
        }
        if (dialect == OpenApiJsonSchemaDialect.Draft4 &&
            name is "additionalItems" or "additionalProperties" &&
            value is JsonValue)
        {
            return value.DeepClone();
        }
        if (Array.IndexOf(s_schemaKeywords, name) >= 0)
        {
            if (name == "items" &&
                dialect != OpenApiJsonSchemaDialect.Draft202012 &&
                value is JsonArray)
            {
                return null;
            }
            return NormalizeSchema(value, dialect);
        }
        if (Array.IndexOf(s_schemaArrayKeywords, name) >= 0)
        {
            var result = new JsonArray();
            foreach (var item in value.AsArray())
            {
                result.Add(NormalizeSchema(item!, dialect));
            }
            return result;
        }
        if (Array.IndexOf(s_schemaMapKeywords, name) >= 0)
        {
            return NormalizeSchemaMap(value, dialect);
        }

        return value.DeepClone();
    }

    private static JsonObject NormalizeSchemaMap(JsonNode value, OpenApiJsonSchemaDialect dialect)
    {
        var result = new JsonObject();
        foreach (var (name, schema) in value.AsObject())
        {
            result[name] = NormalizeSchema(schema!, dialect);
        }
        return result;
    }

    private static void NormalizeExclusiveBound(
        JsonObject source,
        JsonObject result,
        OpenApiJsonSchemaDialect dialect,
        string boundKeyword,
        string exclusiveKeyword)
    {
        if (dialect != OpenApiJsonSchemaDialect.Draft4 ||
            !source.TryGetPropertyValue(exclusiveKeyword, out var exclusive) ||
            exclusive is null ||
            !exclusive.GetValue<bool>())
        {
            return;
        }

        result[exclusiveKeyword] = source[boundKeyword]!.DeepClone();
        result.Remove(boundKeyword);
    }

    private static void NormalizeTuple(
        JsonObject source,
        JsonObject result,
        OpenApiJsonSchemaDialect dialect)
    {
        if (dialect == OpenApiJsonSchemaDialect.Draft202012 ||
            source["items"] is not JsonArray items)
        {
            return;
        }

        var prefixItems = new JsonArray();
        foreach (var item in items)
        {
            prefixItems.Add(NormalizeSchema(item!, dialect));
        }
        result["prefixItems"] = prefixItems;
        result["items"] = source.TryGetPropertyValue("additionalItems", out var additionalItems)
            ? additionalItems is JsonValue
                ? additionalItems.DeepClone()
                : NormalizeSchema(additionalItems!, dialect)
            : JsonValue.Create(true);
    }

    private static void NormalizeDependencies(
        JsonObject source,
        JsonObject result,
        OpenApiJsonSchemaDialect dialect)
    {
        if (dialect is OpenApiJsonSchemaDialect.Draft201909 or
                OpenApiJsonSchemaDialect.Draft202012 ||
            source["dependencies"] is not JsonObject dependencies)
        {
            return;
        }

        var required = new JsonObject();
        var schemas = new JsonObject();
        foreach (var (name, dependency) in dependencies)
        {
            if (dependency is JsonArray)
            {
                required[name] = dependency.DeepClone();
            }
            else
            {
                schemas[name] = NormalizeSchema(dependency!, dialect);
            }
        }
        if (required.Count != 0)
        {
            result["dependentRequired"] = required;
        }
        if (schemas.Count != 0)
        {
            result["dependentSchemas"] = schemas;
        }
    }

    private static void ValidateDialectKeywords(JsonObject schema, OpenApiJsonSchemaDialect dialect)
    {
        foreach (var property in schema)
        {
            var name = property.Key;
            if (name is "$recursiveRef" or "$recursiveAnchor" or "$dynamicRef" or "$dynamicAnchor")
            {
                throw new ArgumentException(Resources.FormatValidatedJsonSchemaDynamicReferenceNotSupported(name));
            }

            if (name == "$vocabulary")
            {
                ValidateVocabulary(schema[name], dialect);
                continue;
            }

            if (!IsKeywordSupported(name, dialect))
            {
                throw KeywordNotSupported(name, dialect);
            }
        }
    }

    private static bool IsKeywordSupported(string name, OpenApiJsonSchemaDialect dialect)
        => dialect switch
        {
            OpenApiJsonSchemaDialect.Draft4 =>
                name is not "$id" and not "$defs" and not "const" and not "contains" and
                not "propertyNames" and not "if" and not "then" and not "else" and
                not "dependentRequired" and not "dependentSchemas" and not "prefixItems" and
                not "unevaluatedItems" and not "unevaluatedProperties" and not "$vocabulary",
            OpenApiJsonSchemaDialect.Draft6 =>
                name is not "id" and not "$defs" and not "if" and not "then" and not "else" and
                not "dependentRequired" and not "dependentSchemas" and not "prefixItems" and
                not "unevaluatedItems" and not "unevaluatedProperties" and not "$vocabulary",
            OpenApiJsonSchemaDialect.Draft7 =>
                name is not "id" and not "$defs" and not "dependentRequired" and
                not "dependentSchemas" and not "prefixItems" and not "unevaluatedItems" and
                not "unevaluatedProperties" and not "$vocabulary",
            OpenApiJsonSchemaDialect.Draft201909 =>
                name is not "id" and not "definitions" and not "prefixItems",
            OpenApiJsonSchemaDialect.Draft202012 =>
                name is not "id" and not "definitions" and not "dependencies" and
                not "additionalItems",
            _ => false,
        };

    private static void ValidateKeywordShapes(JsonObject schema, OpenApiJsonSchemaDialect dialect)
    {
        ValidateString(schema, "$schema", dialect);
        ValidateString(schema, "$id", dialect);
        ValidateString(schema, "id", dialect);
        ValidateString(schema, "$ref", dialect);
        ValidateString(schema, "pattern", dialect);

        foreach (var name in s_schemaMapKeywords)
        {
            ValidateObject(schema, name, dialect);
        }
        ValidateObject(schema, "dependencies", dialect);
        ValidateObject(schema, "dependentRequired", dialect);
        ValidateStringArrayMap(schema, "dependentRequired", dialect);
        ValidateDependencies(schema, dialect);

        foreach (var name in s_schemaArrayKeywords)
        {
            ValidateArray(schema, name, dialect);
        }
        ValidateArray(schema, "required", dialect);
        ValidateArray(schema, "enum", dialect);
        ValidateStringArray(schema, "required", dialect);
        ValidateType(schema, dialect);

        foreach (var name in new[]
        {
            "minimum", "maximum", "multipleOf", "minLength", "maxLength",
            "minItems", "maxItems", "minProperties", "maxProperties",
        })
        {
            ValidateNumber(schema, name, dialect);
        }

        if (dialect == OpenApiJsonSchemaDialect.Draft4)
        {
            ValidateBoolean(schema, "exclusiveMinimum", dialect);
            ValidateBoolean(schema, "exclusiveMaximum", dialect);
            if ((schema.ContainsKey("exclusiveMinimum") && !schema.ContainsKey("minimum")) ||
                (schema.ContainsKey("exclusiveMaximum") && !schema.ContainsKey("maximum")))
            {
                throw new ArgumentException(Resources.FormatValidatedJsonSchemaKeywordInvalid(
                    "exclusiveMinimum/exclusiveMaximum",
                    dialect));
            }
        }
        else
        {
            ValidateNumber(schema, "exclusiveMinimum", dialect);
            ValidateNumber(schema, "exclusiveMaximum", dialect);
        }
    }

    private static void ValidateVocabulary(JsonNode? value, OpenApiJsonSchemaDialect dialect)
    {
        if (dialect is not OpenApiJsonSchemaDialect.Draft201909 and
                not OpenApiJsonSchemaDialect.Draft202012 ||
            value is not JsonObject vocabularies)
        {
            throw KeywordNotSupported("$vocabulary", dialect);
        }

        var expectedPrefix = dialect == OpenApiJsonSchemaDialect.Draft201909
            ? "https://json-schema.org/draft/2019-09/vocab/"
            : "https://json-schema.org/draft/2020-12/vocab/";
        foreach (var (uri, required) in vocabularies)
        {
            if (!uri.StartsWith(expectedPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException(Resources.FormatValidatedJsonSchemaCustomVocabularyNotSupported(uri));
            }
            if (required is not JsonValue requiredValue ||
                !requiredValue.TryGetValue<bool>(out _))
            {
                throw new ArgumentException(Resources.FormatValidatedJsonSchemaKeywordInvalid("$vocabulary", dialect));
            }
        }
    }

    private static void ValidateSchemaReferences(
        JsonElement root,
        JsonElement current,
        OpenApiJsonSchemaDialect dialect)
    {
        if (current.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            if (dialect == OpenApiJsonSchemaDialect.Draft4)
            {
                throw KeywordNotSupported("boolean schema", dialect);
            }
            return;
        }
        if (current.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(Resources.FormatValidatedJsonSchemaKeywordInvalid("schema", dialect));
        }

        foreach (var property in current.EnumerateObject())
        {
            if (property.NameEquals("$ref"))
            {
                var reference = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : null;
                if (reference is null ||
                    !TryResolveLocalReference(root, reference, out var target) ||
                    target.ValueKind is not JsonValueKind.Object and
                        not JsonValueKind.True and not JsonValueKind.False ||
                    dialect == OpenApiJsonSchemaDialect.Draft4 &&
                        target.ValueKind is (JsonValueKind.True or JsonValueKind.False))
                {
                    throw new ArgumentException(Resources.FormatValidatedJsonSchemaExternalReferenceNotSupported(
                        reference ?? string.Empty));
                }
                continue;
            }

            if (Array.IndexOf(s_schemaKeywords, property.Name) >= 0)
            {
                if (property.NameEquals("items") &&
                    dialect != OpenApiJsonSchemaDialect.Draft202012 &&
                    property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        ValidateSchemaReferences(root, item, dialect);
                    }
                }
                else if (dialect == OpenApiJsonSchemaDialect.Draft4 &&
                    property.Name is ("additionalItems" or "additionalProperties") &&
                    property.Value.ValueKind is (JsonValueKind.True or JsonValueKind.False))
                {
                    continue;
                }
                else
                {
                    ValidateSchemaReferences(root, property.Value, dialect);
                }
            }
            else if (Array.IndexOf(s_schemaArrayKeywords, property.Name) >= 0)
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
                foreach (var item in property.Value.EnumerateArray())
                {
                    ValidateSchemaReferences(root, item, dialect);
                }
            }
            else if (Array.IndexOf(s_schemaMapKeywords, property.Name) >= 0)
            {
                ValidateSchemaMapReferences(root, property.Value, dialect);
            }
            else if (property.NameEquals("dependencies") &&
                property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var dependency in property.Value.EnumerateObject())
                {
                    if (dependency.Value.ValueKind != JsonValueKind.Array)
                    {
                        ValidateSchemaReferences(root, dependency.Value, dialect);
                    }
                }
            }
        }
    }

    private static void ValidateSchemaMapReferences(
        JsonElement root,
        JsonElement map,
        OpenApiJsonSchemaDialect dialect)
    {
        if (map.ValueKind != JsonValueKind.Object)
        {
            return;
        }
        foreach (var property in map.EnumerateObject())
        {
            ValidateSchemaReferences(root, property.Value, dialect);
        }
    }

    private static bool TryResolveLocalReference(
        JsonElement root,
        string reference,
        out JsonElement target)
    {
        target = root;
        if (reference == "#")
        {
            return true;
        }
        if (!reference.StartsWith("#/", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var rawSegment in reference.AsSpan(2).ToString().Split('/'))
        {
            var segment = Uri.UnescapeDataString(rawSegment)
                .Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            if (target.ValueKind == JsonValueKind.Object &&
                target.TryGetProperty(segment, out var property))
            {
                target = property;
            }
            else if (target.ValueKind == JsonValueKind.Array &&
                int.TryParse(segment, out var index) &&
                index >= 0 &&
                index < target.GetArrayLength())
            {
                target = target[index];
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private static string RewriteReference(string reference, OpenApiJsonSchemaDialect dialect)
        => IsDefinitionsDialect(dialect) &&
            reference.StartsWith("#/definitions/", StringComparison.Ordinal)
                ? $"#/$defs/{reference["#/definitions/".Length..]}"
                : reference;

    private static ArgumentException KeywordNotSupported(
        string keyword,
        OpenApiJsonSchemaDialect dialect)
        => new(Resources.FormatValidatedJsonSchemaKeywordNotSupportedByDialect(keyword, dialect));

    private static void ValidateString(JsonObject schema, string name, OpenApiJsonSchemaDialect dialect)
        => ValidateNode(schema, name, dialect, static value => value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out _));

    private static void ValidateObject(JsonObject schema, string name, OpenApiJsonSchemaDialect dialect)
        => ValidateNode(schema, name, dialect, static value => value is JsonObject);

    private static void ValidateArray(JsonObject schema, string name, OpenApiJsonSchemaDialect dialect)
        => ValidateNode(schema, name, dialect, static value => value is JsonArray);

    private static void ValidateBoolean(JsonObject schema, string name, OpenApiJsonSchemaDialect dialect)
        => ValidateNode(schema, name, dialect, static value => value is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out _));

    private static void ValidateNumber(JsonObject schema, string name, OpenApiJsonSchemaDialect dialect)
        => ValidateNode(
            schema,
            name,
            dialect,
            static value => value is JsonValue jsonValue &&
                (jsonValue.TryGetValue<decimal>(out _) || jsonValue.TryGetValue<double>(out _)));

    private static void ValidateStringArray(
        JsonObject schema,
        string name,
        OpenApiJsonSchemaDialect dialect)
        => ValidateNode(
            schema,
            name,
            dialect,
            static value => value is JsonArray array && IsStringArray(array));

    private static void ValidateStringArrayMap(
        JsonObject schema,
        string name,
        OpenApiJsonSchemaDialect dialect)
    {
        if (!schema.TryGetPropertyValue(name, out var value) || value is not JsonObject map)
        {
            return;
        }
        foreach (var (_, member) in map)
        {
            if (member is not JsonArray array ||
                !IsStringArray(array))
            {
                throw new ArgumentException(Resources.FormatValidatedJsonSchemaKeywordInvalid(name, dialect));
            }
        }
    }

    private static void ValidateDependencies(JsonObject schema, OpenApiJsonSchemaDialect dialect)
    {
        if (!schema.TryGetPropertyValue("dependencies", out var value) ||
            value is not JsonObject dependencies)
        {
            return;
        }
        foreach (var (_, dependency) in dependencies)
        {
            if (dependency is JsonArray array &&
                !IsStringArray(array))
            {
                throw new ArgumentException(Resources.FormatValidatedJsonSchemaKeywordInvalid(
                    "dependencies",
                    dialect));
            }
        }
    }

    private static void ValidateType(JsonObject schema, OpenApiJsonSchemaDialect dialect)
    {
        if (!schema.TryGetPropertyValue("type", out var value))
        {
            return;
        }

        static bool IsType(JsonNode? node)
            => node is JsonValue jsonValue &&
                jsonValue.TryGetValue<string>(out var type) &&
                type is "null" or "boolean" or "object" or "array" or
                    "number" or "string" or "integer";

        if (!IsType(value) &&
            (value is not JsonArray types || types.Count == 0 || !AllTypes(types)))
        {
            throw new ArgumentException(Resources.FormatValidatedJsonSchemaKeywordInvalid("type", dialect));
        }

        static bool AllTypes(JsonArray types)
        {
            foreach (var type in types)
            {
                if (!IsType(type))
                {
                    return false;
                }
            }
            return true;
        }
    }

    private static bool IsStringArray(JsonArray array)
    {
        foreach (var item in array)
        {
            if (item is not JsonValue jsonValue ||
                !jsonValue.TryGetValue<string>(out _))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsDefinitionsDialect(OpenApiJsonSchemaDialect dialect)
        => dialect is OpenApiJsonSchemaDialect.Draft4 or
            OpenApiJsonSchemaDialect.Draft6 or
            OpenApiJsonSchemaDialect.Draft7;

    private static void ValidateNode(
        JsonObject schema,
        string name,
        OpenApiJsonSchemaDialect dialect,
        Func<JsonNode, bool> predicate)
    {
        if (schema.TryGetPropertyValue(name, out var value) &&
            (value is null || !predicate(value)))
        {
            throw new ArgumentException(Resources.FormatValidatedJsonSchemaKeywordInvalid(name, dialect));
        }
    }
}
