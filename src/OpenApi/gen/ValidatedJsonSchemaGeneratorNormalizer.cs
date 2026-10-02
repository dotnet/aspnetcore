// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.OpenApi.SourceGenerators.Json;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

internal enum GeneratedSchemaDialect
{
    Draft202012,
    Draft4,
    Draft6,
    Draft7,
    Draft201909,
}

internal sealed class SchemaGenerationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

internal sealed record NormalizedGeneratedSchema(
    JsonValue Source,
    JsonValue Normalized,
    GeneratedSchemaDialect Dialect,
    IReadOnlyList<string> LocalReferences);

internal static class ValidatedJsonSchemaGeneratorNormalizer
{
    private const string CanonicalDialectUri = "https://json-schema.org/draft/2020-12/schema";

    private static readonly HashSet<string> s_schemaKeywords = new(StringComparer.Ordinal)
    {
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
    };

    private static readonly HashSet<string> s_schemaArrayKeywords = new(StringComparer.Ordinal)
    {
        "allOf",
        "anyOf",
        "oneOf",
        "prefixItems",
    };

    private static readonly HashSet<string> s_schemaMapKeywords = new(StringComparer.Ordinal)
    {
        "$defs",
        "definitions",
        "dependentSchemas",
        "patternProperties",
        "properties",
    };

    public static NormalizedGeneratedSchema Normalize(string source, GeneratedSchemaDialect dialect)
    {
        JsonValue schema;
        try
        {
            schema = JsonValue.Parse(source);
        }
        catch (FormatException exception)
        {
            throw new SchemaGenerationException("OASGEN001", exception.Message);
        }

        ValidateDialect(schema, dialect);
        var references = new SortedSet<string>(StringComparer.Ordinal);
        ValidateReferences(schema, schema, dialect, references);
        var normalized = NormalizeSchema(schema, dialect, isRoot: true);
        return new(schema, normalized, dialect, new List<string>(references));
    }

    public static GeneratedSchemaDialect ParseDialect(string value)
        => value switch
        {
            "Draft4" => GeneratedSchemaDialect.Draft4,
            "Draft6" => GeneratedSchemaDialect.Draft6,
            "Draft7" => GeneratedSchemaDialect.Draft7,
            "Draft201909" => GeneratedSchemaDialect.Draft201909,
            "Draft202012" => GeneratedSchemaDialect.Draft202012,
            _ => throw new SchemaGenerationException(
                "OASGEN002",
                $"The JSON Schema dialect '{value}' is not supported. Use Draft4, Draft6, Draft7, Draft201909, or Draft202012."),
        };

    public static string GetDialectUri(GeneratedSchemaDialect dialect)
        => dialect switch
        {
            GeneratedSchemaDialect.Draft4 => "http://json-schema.org/draft-04/schema#",
            GeneratedSchemaDialect.Draft6 => "http://json-schema.org/draft-06/schema#",
            GeneratedSchemaDialect.Draft7 => "http://json-schema.org/draft-07/schema#",
            GeneratedSchemaDialect.Draft201909 => "https://json-schema.org/draft/2019-09/schema",
            GeneratedSchemaDialect.Draft202012 => CanonicalDialectUri,
            _ => throw new ArgumentOutOfRangeException(nameof(dialect)),
        };

    private static void ValidateDialect(JsonValue schema, GeneratedSchemaDialect dialect)
    {
        if (schema is JsonBoolean)
        {
            if (dialect == GeneratedSchemaDialect.Draft4)
            {
                throw Unsupported("boolean schema", dialect);
            }
            return;
        }

        if (schema is not JsonObject schemaObject ||
            !schemaObject.Properties.TryGetValue("$schema", out var declared) ||
            declared is not JsonString declaredString ||
            !string.Equals(declaredString.Value, GetDialectUri(dialect), StringComparison.Ordinal))
        {
            throw new SchemaGenerationException(
                "OASGEN003",
                $"The root schema must declare the exact dialect URI '{GetDialectUri(dialect)}'.");
        }
    }

    private static JsonValue NormalizeSchema(
        JsonValue schema,
        GeneratedSchemaDialect dialect,
        bool isRoot = false)
    {
        if (schema is JsonBoolean)
        {
            if (dialect == GeneratedSchemaDialect.Draft4)
            {
                throw Unsupported("boolean schema", dialect);
            }
            return Clone(schema);
        }
        if (schema is not JsonObject source)
        {
            throw Invalid("schema", dialect);
        }

        ValidateKeywords(source, dialect);
        ValidateShapes(source, dialect);

        if (IsLegacyRefDialect(dialect) &&
            source.Properties.TryGetValue("$ref", out var legacyReference))
        {
            var referenceOnly = new JsonObject();
            var referenceArray = new JsonArray();
            var referenceObject = new JsonObject();
            referenceObject.Properties["$ref"] = new JsonString(RewriteReference(
                ((JsonString)legacyReference).Value,
                dialect));
            referenceArray.Items.Add(referenceObject);
            referenceOnly.Properties["allOf"] = referenceArray;
            if (source.Properties.TryGetValue("definitions", out var definitions))
            {
                referenceOnly.Properties["$defs"] = NormalizeSchemaMap(definitions, dialect);
            }
            var idKeyword = dialect == GeneratedSchemaDialect.Draft4 ? "id" : "$id";
            if (source.Properties.TryGetValue(idKeyword, out var id))
            {
                referenceOnly.Properties["$id"] = Clone(id);
            }
            if (isRoot)
            {
                referenceOnly.Properties["$schema"] = new JsonString(CanonicalDialectUri);
            }
            return referenceOnly;
        }

        var result = new JsonObject();
        foreach (var property in source.Properties)
        {
            var name = property.Key;
            if (name is "id" or "definitions" or "dependencies" or "additionalItems" or "$vocabulary" ||
                dialect == GeneratedSchemaDialect.Draft4 &&
                    name is "exclusiveMinimum" or "exclusiveMaximum")
            {
                continue;
            }
            var normalized = NormalizeKeywordValue(name, property.Value, dialect);
            if (normalized is not null)
            {
                result.Properties[name] = normalized;
            }
        }

        if (isRoot)
        {
            result.Properties["$schema"] = new JsonString(CanonicalDialectUri);
        }
        if (dialect == GeneratedSchemaDialect.Draft4 &&
            source.Properties.TryGetValue("id", out var legacyId))
        {
            result.Properties["$id"] = Clone(legacyId);
        }
        if (IsDefinitionsDialect(dialect) &&
            source.Properties.TryGetValue("definitions", out var definitionsValue))
        {
            result.Properties["$defs"] = NormalizeSchemaMap(definitionsValue, dialect);
        }

        NormalizeExclusiveBound(source, result, dialect, "minimum", "exclusiveMinimum");
        NormalizeExclusiveBound(source, result, dialect, "maximum", "exclusiveMaximum");
        NormalizeTuple(source, result, dialect);
        NormalizeDependencies(source, result, dialect);
        return result;
    }

    private static JsonValue? NormalizeKeywordValue(
        string name,
        JsonValue value,
        GeneratedSchemaDialect dialect)
    {
        if (name == "$ref")
        {
            return new JsonString(RewriteReference(((JsonString)value).Value, dialect));
        }
        if (s_schemaKeywords.Contains(name))
        {
            if (name == "items" && dialect != GeneratedSchemaDialect.Draft202012 && value is JsonArray)
            {
                return null;
            }
            if (value is JsonBoolean && name is "additionalItems" or "additionalProperties")
            {
                return Clone(value);
            }
            return NormalizeSchema(value, dialect);
        }
        if (s_schemaArrayKeywords.Contains(name))
        {
            var result = new JsonArray();
            foreach (var item in ((JsonArray)value).Items)
            {
                result.Items.Add(NormalizeSchema(item, dialect));
            }
            return result;
        }
        if (s_schemaMapKeywords.Contains(name))
        {
            return NormalizeSchemaMap(value, dialect);
        }
        return Clone(value);
    }

    private static JsonObject NormalizeSchemaMap(JsonValue value, GeneratedSchemaDialect dialect)
    {
        var result = new JsonObject();
        foreach (var property in ((JsonObject)value).Properties)
        {
            result.Properties[property.Key] = NormalizeSchema(property.Value, dialect);
        }
        return result;
    }

    private static void NormalizeExclusiveBound(
        JsonObject source,
        JsonObject result,
        GeneratedSchemaDialect dialect,
        string boundKeyword,
        string exclusiveKeyword)
    {
        if (dialect == GeneratedSchemaDialect.Draft4 &&
            source.Properties.TryGetValue(exclusiveKeyword, out var exclusive) &&
            exclusive is JsonBoolean { Value: true })
        {
            result.Properties[exclusiveKeyword] = Clone(source.Properties[boundKeyword]);
            result.Properties.Remove(boundKeyword);
        }
    }

    private static void NormalizeTuple(
        JsonObject source,
        JsonObject result,
        GeneratedSchemaDialect dialect)
    {
        if (dialect == GeneratedSchemaDialect.Draft202012 ||
            !source.Properties.TryGetValue("items", out var itemsValue) ||
            itemsValue is not JsonArray items)
        {
            return;
        }

        var prefixItems = new JsonArray();
        foreach (var item in items.Items)
        {
            prefixItems.Items.Add(NormalizeSchema(item, dialect));
        }
        result.Properties["prefixItems"] = prefixItems;
        result.Properties["items"] = source.Properties.TryGetValue("additionalItems", out var additional)
            ? additional is JsonBoolean ? Clone(additional) : NormalizeSchema(additional, dialect)
            : JsonBoolean.True;
    }

    private static void NormalizeDependencies(
        JsonObject source,
        JsonObject result,
        GeneratedSchemaDialect dialect)
    {
        if (dialect is GeneratedSchemaDialect.Draft201909 or GeneratedSchemaDialect.Draft202012 ||
            !source.Properties.TryGetValue("dependencies", out var dependenciesValue) ||
            dependenciesValue is not JsonObject dependencies)
        {
            return;
        }

        var required = new JsonObject();
        var schemas = new JsonObject();
        foreach (var dependency in dependencies.Properties)
        {
            if (dependency.Value is JsonArray)
            {
                required.Properties[dependency.Key] = Clone(dependency.Value);
            }
            else
            {
                schemas.Properties[dependency.Key] = NormalizeSchema(dependency.Value, dialect);
            }
        }
        if (required.Properties.Count != 0)
        {
            result.Properties["dependentRequired"] = required;
        }
        if (schemas.Properties.Count != 0)
        {
            result.Properties["dependentSchemas"] = schemas;
        }
    }

    private static void ValidateKeywords(JsonObject schema, GeneratedSchemaDialect dialect)
    {
        foreach (var property in schema.Properties)
        {
            var name = property.Key;
            if (name is "$recursiveRef" or "$recursiveAnchor" or "$dynamicRef" or "$dynamicAnchor")
            {
                throw new SchemaGenerationException(
                    "OASGEN004",
                    $"The dynamic or recursive reference keyword '{name}' cannot be generated safely.");
            }
            if (name == "$vocabulary")
            {
                ValidateVocabulary(property.Value, dialect);
                continue;
            }
            if (!IsKeywordSupported(name, dialect))
            {
                throw Unsupported(name, dialect);
            }
        }
    }

    private static bool IsKeywordSupported(string name, GeneratedSchemaDialect dialect)
        => dialect switch
        {
            GeneratedSchemaDialect.Draft4 =>
                name is not "$id" and not "$defs" and not "const" and not "contains" and
                not "propertyNames" and not "if" and not "then" and not "else" and
                not "dependentRequired" and not "dependentSchemas" and not "prefixItems" and
                not "unevaluatedItems" and not "unevaluatedProperties" and not "$vocabulary",
            GeneratedSchemaDialect.Draft6 =>
                name is not "id" and not "$defs" and not "if" and not "then" and not "else" and
                not "dependentRequired" and not "dependentSchemas" and not "prefixItems" and
                not "unevaluatedItems" and not "unevaluatedProperties" and not "$vocabulary",
            GeneratedSchemaDialect.Draft7 =>
                name is not "id" and not "$defs" and not "dependentRequired" and
                not "dependentSchemas" and not "prefixItems" and not "unevaluatedItems" and
                not "unevaluatedProperties" and not "$vocabulary",
            GeneratedSchemaDialect.Draft201909 =>
                name is not "id" and not "definitions" and not "prefixItems",
            GeneratedSchemaDialect.Draft202012 =>
                name is not "id" and not "definitions" and not "dependencies" and
                not "additionalItems",
            _ => false,
        };

    private static void ValidateShapes(JsonObject schema, GeneratedSchemaDialect dialect)
    {
        ValidateString(schema, "$schema", dialect);
        ValidateString(schema, "$id", dialect);
        ValidateString(schema, "id", dialect);
        ValidateString(schema, "$ref", dialect);
        foreach (var name in s_schemaMapKeywords)
        {
            Validate<JsonObject>(schema, name, dialect);
        }
        Validate<JsonObject>(schema, "dependencies", dialect);
        Validate<JsonObject>(schema, "dependentRequired", dialect);
        foreach (var name in s_schemaArrayKeywords)
        {
            Validate<JsonArray>(schema, name, dialect);
        }
        Validate<JsonArray>(schema, "required", dialect);
        Validate<JsonArray>(schema, "enum", dialect);
        ValidateStringArray(schema, "required", dialect);
        ValidateType(schema, dialect);

        foreach (var name in new[]
        {
            "minimum", "maximum", "multipleOf", "minLength", "maxLength",
            "minItems", "maxItems", "minProperties", "maxProperties",
        })
        {
            Validate<JsonNumber>(schema, name, dialect);
        }

        if (dialect == GeneratedSchemaDialect.Draft4)
        {
            Validate<JsonBoolean>(schema, "exclusiveMinimum", dialect);
            Validate<JsonBoolean>(schema, "exclusiveMaximum", dialect);
            if (schema.Properties.ContainsKey("exclusiveMinimum") && !schema.Properties.ContainsKey("minimum") ||
                schema.Properties.ContainsKey("exclusiveMaximum") && !schema.Properties.ContainsKey("maximum"))
            {
                throw Invalid("exclusiveMinimum/exclusiveMaximum", dialect);
            }
        }
        else
        {
            Validate<JsonNumber>(schema, "exclusiveMinimum", dialect);
            Validate<JsonNumber>(schema, "exclusiveMaximum", dialect);
        }

        if (schema.Properties.TryGetValue("dependencies", out var dependenciesValue))
        {
            foreach (var dependency in ((JsonObject)dependenciesValue).Properties)
            {
                if (dependency.Value is JsonArray array && !IsStringArray(array))
                {
                    throw Invalid("dependencies", dialect);
                }
            }
        }
        if (schema.Properties.TryGetValue("dependentRequired", out var dependentRequiredValue))
        {
            foreach (var dependency in ((JsonObject)dependentRequiredValue).Properties)
            {
                if (dependency.Value is not JsonArray array || !IsStringArray(array))
                {
                    throw Invalid("dependentRequired", dialect);
                }
            }
        }
    }

    private static void ValidateVocabulary(JsonValue value, GeneratedSchemaDialect dialect)
    {
        if (dialect is not GeneratedSchemaDialect.Draft201909 and
                not GeneratedSchemaDialect.Draft202012 ||
            value is not JsonObject vocabularies)
        {
            throw Unsupported("$vocabulary", dialect);
        }

        var prefix = dialect == GeneratedSchemaDialect.Draft201909
            ? "https://json-schema.org/draft/2019-09/vocab/"
            : "https://json-schema.org/draft/2020-12/vocab/";
        foreach (var vocabulary in vocabularies.Properties)
        {
            if (!vocabulary.Key.StartsWith(prefix, StringComparison.Ordinal) ||
                vocabulary.Value is not JsonBoolean)
            {
                throw new SchemaGenerationException(
                    "OASGEN005",
                    $"The custom vocabulary '{vocabulary.Key}' is not supported.");
            }
        }
    }

    private static void ValidateReferences(
        JsonValue root,
        JsonValue current,
        GeneratedSchemaDialect dialect,
        ISet<string> references)
    {
        if (current is JsonBoolean)
        {
            return;
        }
        if (current is not JsonObject schema)
        {
            throw Invalid("schema", dialect);
        }

        foreach (var property in schema.Properties)
        {
            if (property.Key == "$ref")
            {
                var reference = property.Value is JsonString value ? value.Value : string.Empty;
                if (!TryResolve(root, reference, out var target) ||
                    target is not JsonObject and not JsonBoolean ||
                    dialect == GeneratedSchemaDialect.Draft4 && target is JsonBoolean)
                {
                    throw new SchemaGenerationException(
                        "OASGEN006",
                        $"The reference '{reference}' is external, unresolved, or unsupported.");
                }
                references.Add(RewriteReference(reference, dialect));
                continue;
            }
            if (s_schemaKeywords.Contains(property.Key))
            {
                if (property.Key == "items" &&
                    dialect != GeneratedSchemaDialect.Draft202012 &&
                    property.Value is JsonArray tuple)
                {
                    foreach (var item in tuple.Items)
                    {
                        ValidateReferences(root, item, dialect, references);
                    }
                }
                else if (property.Value is not JsonBoolean)
                {
                    ValidateReferences(root, property.Value, dialect, references);
                }
            }
            else if (s_schemaArrayKeywords.Contains(property.Key) &&
                property.Value is JsonArray array)
            {
                foreach (var item in array.Items)
                {
                    ValidateReferences(root, item, dialect, references);
                }
            }
            else if (s_schemaMapKeywords.Contains(property.Key) &&
                property.Value is JsonObject map)
            {
                foreach (var item in map.Properties.Values)
                {
                    ValidateReferences(root, item, dialect, references);
                }
            }
            else if (property.Key == "dependencies" && property.Value is JsonObject dependencies)
            {
                foreach (var dependency in dependencies.Properties.Values)
                {
                    if (dependency is not JsonArray)
                    {
                        ValidateReferences(root, dependency, dialect, references);
                    }
                }
            }
        }
    }

    private static bool TryResolve(JsonValue root, string reference, out JsonValue target)
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

        foreach (var rawSegment in reference.Substring(2).Split('/'))
        {
            var segment = Uri.UnescapeDataString(rawSegment)
                .Replace("~1", "/")
                .Replace("~0", "~");
            if (target is JsonObject objectTarget &&
                objectTarget.Properties.TryGetValue(segment, out var property))
            {
                target = property;
            }
            else if (target is JsonArray arrayTarget &&
                int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index) &&
                index >= 0 && index < arrayTarget.Items.Count)
            {
                target = arrayTarget.Items[index];
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private static void ValidateType(JsonObject schema, GeneratedSchemaDialect dialect)
    {
        if (!schema.Properties.TryGetValue("type", out var value))
        {
            return;
        }
        if (value is JsonString type)
        {
            if (!IsType(type.Value))
            {
                throw Invalid("type", dialect);
            }
            return;
        }
        if (value is not JsonArray types || types.Items.Count == 0)
        {
            throw Invalid("type", dialect);
        }
        foreach (var item in types.Items)
        {
            if (item is not JsonString itemType || !IsType(itemType.Value))
            {
                throw Invalid("type", dialect);
            }
        }
    }

    private static bool IsType(string value)
        => value is "null" or "boolean" or "object" or "array" or
            "number" or "string" or "integer";

    private static void ValidateStringArray(
        JsonObject schema,
        string name,
        GeneratedSchemaDialect dialect)
    {
        if (schema.Properties.TryGetValue(name, out var value) &&
            (value is not JsonArray array || !IsStringArray(array)))
        {
            throw Invalid(name, dialect);
        }
    }

    private static bool IsStringArray(JsonArray array)
    {
        foreach (var item in array.Items)
        {
            if (item is not JsonString)
            {
                return false;
            }
        }
        return true;
    }

    private static void ValidateString(
        JsonObject schema,
        string name,
        GeneratedSchemaDialect dialect)
        => Validate<JsonString>(schema, name, dialect);

    private static void Validate<T>(
        JsonObject schema,
        string name,
        GeneratedSchemaDialect dialect)
        where T : JsonValue
    {
        if (schema.Properties.TryGetValue(name, out var value) && value is not T)
        {
            throw Invalid(name, dialect);
        }
    }

    private static string RewriteReference(string reference, GeneratedSchemaDialect dialect)
        => IsDefinitionsDialect(dialect) &&
            reference.StartsWith("#/definitions/", StringComparison.Ordinal)
                ? $"#/$defs/{reference.Substring("#/definitions/".Length)}"
                : reference;

    private static bool IsDefinitionsDialect(GeneratedSchemaDialect dialect)
        => dialect is GeneratedSchemaDialect.Draft4 or
            GeneratedSchemaDialect.Draft6 or
            GeneratedSchemaDialect.Draft7;

    private static bool IsLegacyRefDialect(GeneratedSchemaDialect dialect)
        => dialect is GeneratedSchemaDialect.Draft4 or
            GeneratedSchemaDialect.Draft6 or
            GeneratedSchemaDialect.Draft7;

    private static JsonValue Clone(JsonValue value)
        => JsonValue.Parse(value.ToCanonicalJson());

    private static SchemaGenerationException Unsupported(
        string keyword,
        GeneratedSchemaDialect dialect)
        => new(
            "OASGEN007",
            $"The keyword '{keyword}' is not supported by the declared {dialect} dialect.");

    private static SchemaGenerationException Invalid(
        string keyword,
        GeneratedSchemaDialect dialect)
        => new(
            "OASGEN008",
            $"The keyword '{keyword}' has an invalid shape for the declared {dialect} dialect.");
}
