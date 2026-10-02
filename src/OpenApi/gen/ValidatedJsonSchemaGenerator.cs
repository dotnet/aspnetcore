// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.OpenApi.SourceGenerators.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class ValidatedJsonSchemaGenerator : IIncrementalGenerator
{
    private const string ItemMarker = "build_metadata.AdditionalFiles.OpenApiValidatedJsonSchema";
    private const string LogicalName = "build_metadata.AdditionalFiles.LogicalName";
    private const string Dialect = "build_metadata.AdditionalFiles.Dialect";
    private const string Capabilities = "build_metadata.AdditionalFiles.Capabilities";
    private const string SchemaIdentity = "build_metadata.AdditionalFiles.SchemaIdentity";
    private const string ClrType = "build_metadata.AdditionalFiles.ClrType";
    private const string ValidatorType = "build_metadata.AdditionalFiles.ValidatorType";
    private const string ValidatorConfigurationIdentity =
        "build_metadata.AdditionalFiles.ValidatorConfigurationIdentity";

#pragma warning disable RS2008 // Prototype diagnostics are not shipped analyzer rules.
    private static readonly DiagnosticDescriptor s_invalidSchema = new(
        "OASGEN001",
        "Invalid generated JSON Schema",
        "{0}",
        "OpenApi",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_invalidMetadata = new(
        "OASGEN002",
        "Invalid generated JSON Schema metadata",
        "{0}",
        "OpenApi",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_duplicateName = new(
        "OASGEN009",
        "Duplicate generated JSON Schema name",
        "The generated JSON Schema logical name '{0}' is used by more than one AdditionalFile",
        "OpenApi",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
#pragma warning restore RS2008

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var inputs = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, cancellationToken) => CreateInput(pair.Left, pair.Right, cancellationToken))
            .Where(static input => input is not null)
            .Collect();

        context.RegisterSourceOutput(inputs, static (productionContext, schemas) =>
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var schema in schemas.OrderBy(
                static schema => schema!.LogicalName ?? string.Empty,
                StringComparer.Ordinal))
            {
                var input = schema!;
                var logicalName = input.LogicalName ?? string.Empty;
                if (!names.Add(logicalName))
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(
                        s_duplicateName,
                        Location.None,
                        logicalName));
                    continue;
                }
                Emit(productionContext, input);
            }
        });
    }

    private static SchemaInput? CreateInput(
        AdditionalText file,
        AnalyzerConfigOptionsProvider optionsProvider,
        CancellationToken cancellationToken)
    {
        var options = optionsProvider.GetOptions(file);
        if (!options.TryGetValue(ItemMarker, out var marker) ||
            !bool.TryParse(marker, out var enabled) ||
            !enabled)
        {
            return null;
        }

        var source = file.GetText(cancellationToken)?.ToString();
        options.TryGetValue(LogicalName, out var logicalName);
        options.TryGetValue(Dialect, out var dialect);
        options.TryGetValue(Capabilities, out var capabilities);
        options.TryGetValue(SchemaIdentity, out var schemaIdentity);
        options.TryGetValue(ClrType, out var clrType);
        options.TryGetValue(ValidatorType, out var validatorType);
        options.TryGetValue(ValidatorConfigurationIdentity, out var validatorConfigurationIdentity);
        return new(
            file.Path,
            source,
            logicalName,
            dialect,
            capabilities,
            schemaIdentity,
            clrType,
            validatorType,
            validatorConfigurationIdentity);
    }

    private static void Emit(SourceProductionContext context, SchemaInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Source) ||
            string.IsNullOrWhiteSpace(input.LogicalName) ||
            string.IsNullOrWhiteSpace(input.Dialect))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_invalidMetadata,
                Location.None,
                $"The schema item '{input.FileName}' requires non-empty LogicalName and Dialect metadata and non-empty content."));
            return;
        }

        var sourceText = input.Source!;
        var logicalName = input.LogicalName!;
        var dialectName = input.Dialect!;
        var typeName = SanitizeIdentifier(logicalName);
        if (typeName.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_invalidMetadata,
                Location.None,
                $"The logical name '{logicalName}' does not contain a valid identifier character."));
            return;
        }

        GeneratedSchemaDialect dialect;
        NormalizedGeneratedSchema normalized;
        try
        {
            dialect = ValidatedJsonSchemaGeneratorNormalizer.ParseDialect(dialectName);
            normalized = ValidatedJsonSchemaGeneratorNormalizer.Normalize(sourceText, dialect);
        }
        catch (SchemaGenerationException exception)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                exception.Code == "OASGEN002" ? s_invalidMetadata : s_invalidSchema,
                Location.None,
                exception.Message));
            return;
        }

        var capabilityValue = input.Capabilities switch
        {
            null or "" or "None" => 0,
            "FormatAssertions" => 1,
            _ => -1,
        };
        if (capabilityValue < 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_invalidMetadata,
                Location.None,
                $"The capabilities value '{input.Capabilities}' must be None or FormatAssertions."));
            return;
        }

        var sourceBytes = Encoding.UTF8.GetBytes(sourceText);
        var normalizedBytes = Encoding.UTF8.GetBytes(normalized.Normalized.ToCanonicalJson());
        var referencesBytes = Encoding.UTF8.GetBytes(CreateReferencesJson(normalized.LocalReferences));
        var schemaIdentity = input.SchemaIdentity?.Trim() ?? string.Empty;
        if (schemaIdentity.Length == 0)
        {
            schemaIdentity = Hash(sourceBytes);
        }
        else if (schemaIdentity.Length != 64 || schemaIdentity.Any(static character =>
            !((character >= '0' && character <= '9') ||
              (character >= 'a' && character <= 'f') ||
              (character >= 'A' && character <= 'F'))))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_invalidMetadata,
                Location.None,
                "SchemaIdentity must be a 64-character hexadecimal SHA-256 value."));
            return;
        }
        var source = new StringBuilder(
            """
            // <auto-generated />
            #nullable enable
            #pragma warning disable ASP0040
            namespace Microsoft.AspNetCore.OpenApi.Generated;

            """);
        source.Append("public sealed class ").Append(typeName)
            .AppendLine("Artifact : global::Microsoft.AspNetCore.OpenApi.IOpenApiValidatedJsonSchemaArtifact<")
            .Append("    ").Append(typeName).AppendLine("Artifact>")
            .AppendLine("{");
        EmitByteArray(source, "s_sourceSchema", sourceBytes);
        EmitByteArray(source, "s_normalizedSchema", normalizedBytes);
        EmitByteArray(source, "s_localReferences", referencesBytes);
        source.AppendLine("    public static global::System.ReadOnlyMemory<byte> SourceSchema => s_sourceSchema;")
            .AppendLine("    public static global::System.ReadOnlyMemory<byte> NormalizedSchema => s_normalizedSchema;")
            .AppendLine("    public static global::System.ReadOnlyMemory<byte> LocalReferences => s_localReferences;")
            .Append("    public static global::Microsoft.AspNetCore.OpenApi.OpenApiJsonSchemaDialect Dialect => ")
            .Append("global::Microsoft.AspNetCore.OpenApi.OpenApiJsonSchemaDialect.")
            .Append(input.Dialect).AppendLine(";")
            .Append("    public static global::Microsoft.AspNetCore.OpenApi.OpenApiJsonSchemaValidationCapabilities Capabilities => ")
            .Append(capabilityValue == 0
                ? "global::Microsoft.AspNetCore.OpenApi.OpenApiJsonSchemaValidationCapabilities.None"
                : "global::Microsoft.AspNetCore.OpenApi.OpenApiJsonSchemaValidationCapabilities.FormatAssertions")
            .AppendLine(";")
            .Append("    public static string SchemaIdentity => \"").Append(schemaIdentity).AppendLine("\";")
            .AppendLine("    public static global::Microsoft.OpenApi.OpenApiSchema CreateOpenApiSchema(")
            .AppendLine("        global::Microsoft.OpenApi.OpenApiSpecVersion openApiVersion)")
            .AppendLine("    {")
            .AppendLine("        _ = openApiVersion;");
        EmitSchema(source, normalized.Normalized, "        return ", 2, ";");
        source.AppendLine("    }")
            .AppendLine("}");

        if (!string.IsNullOrWhiteSpace(input.ValidatorType) ||
            !string.IsNullOrWhiteSpace(input.ClrType) ||
            !string.IsNullOrWhiteSpace(input.ValidatorConfigurationIdentity))
        {
            if (string.IsNullOrWhiteSpace(input.ValidatorType) ||
                string.IsNullOrWhiteSpace(input.ClrType) ||
                string.IsNullOrWhiteSpace(input.ValidatorConfigurationIdentity))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_invalidMetadata,
                    Location.None,
                    "ClrType, ValidatorType, and ValidatorConfigurationIdentity must either all be supplied or all be omitted."));
                return;
            }
            var configurationIdentity = input.ValidatorConfigurationIdentity!;
            var identity = CompositeHash(sourceBytes, (int)dialect, capabilityValue, configurationIdentity);
            source.Append("public sealed class ").Append(typeName)
                .AppendLine("Binding : global::Microsoft.AspNetCore.OpenApi.IOpenApiValidatedJsonSchemaBinding<")
                .Append("    ").Append(typeName).AppendLine("Binding>")
                .AppendLine("{")
                .Append("    public static global::System.Type Type => typeof(").Append(input.ClrType).AppendLine(");")
                .Append("    public static string SchemaIdentity => ").Append(typeName).AppendLine("Artifact.SchemaIdentity;")
                .Append("    public static string Identity => \"").Append(identity).AppendLine("\";")
                .Append("    public static global::Microsoft.AspNetCore.OpenApi.OpenApiJsonSchemaDialect Dialect => ")
                .Append(typeName).AppendLine("Artifact.Dialect;")
                .Append("    public static global::Microsoft.AspNetCore.OpenApi.OpenApiJsonSchemaValidationCapabilities Capabilities => ")
                .Append(typeName).AppendLine("Artifact.Capabilities;")
                .AppendLine("    public static global::Microsoft.OpenApi.OpenApiSchema CreateOpenApiSchema(")
                .AppendLine("        global::Microsoft.OpenApi.OpenApiSpecVersion openApiVersion)")
                .AppendLine("    {")
                .Append("        var schema = ").Append(typeName).AppendLine("Artifact.CreateOpenApiSchema(openApiVersion);")
                .AppendLine("        schema.Metadata ??= new global::System.Collections.Generic.Dictionary<string, object>();")
                .AppendLine("        schema.Metadata[\"x-schema-validated-identity\"] = Identity;")
                .AppendLine("        return schema;")
                .AppendLine("    }")
                .AppendLine("    public static global::System.Threading.Tasks.ValueTask<global::Microsoft.AspNetCore.OpenApi.OpenApiJsonSchemaValidationResult> ValidateAsync(")
                .AppendLine("        global::System.ReadOnlyMemory<byte> utf8Json,")
                .AppendLine("        global::Microsoft.AspNetCore.OpenApi.OpenApiSchemaEvidencePurpose purpose,")
                .AppendLine("        global::System.Threading.CancellationToken cancellationToken = default)")
                .Append("        => ").Append(input.ValidatorType)
                .AppendLine(".ValidateAsync(utf8Json, purpose, cancellationToken);")
                .AppendLine("}");
        }

        context.AddSource($"{typeName}.ValidatedJsonSchema.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static void EmitSchema(
        StringBuilder source,
        JsonValue schema,
        string prefix,
        int indent,
        string terminator = ",")
    {
        if (schema is JsonBoolean { Value: false })
        {
            source.Append(prefix).AppendLine("new global::Microsoft.OpenApi.OpenApiSchema")
                .Append(' ', indent * 4).AppendLine("{")
                .Append(' ', (indent + 1) * 4).AppendLine("Not = new global::Microsoft.OpenApi.OpenApiSchema(),")
                .Append(' ', indent * 4).Append('}').AppendLine(terminator);
            return;
        }
        if (schema is JsonBoolean)
        {
            source.Append(prefix).Append("new global::Microsoft.OpenApi.OpenApiSchema()").AppendLine(terminator);
            return;
        }

        var value = (JsonObject)schema;
        source.Append(prefix).AppendLine("new global::Microsoft.OpenApi.OpenApiSchema")
            .Append(' ', indent * 4).AppendLine("{");
        if (value.Properties.TryGetValue("type", out var type))
        {
            source.Append(' ', (indent + 1) * 4).Append("Type = ").Append(EmitType(type)).AppendLine(",");
        }
        EmitStringProperty(source, value, "title", "Title", indent);
        EmitStringProperty(source, value, "description", "Description", indent);
        EmitStringProperty(source, value, "format", "Format", indent);
        EmitStringProperty(source, value, "pattern", "Pattern", indent);
        EmitStringProperty(source, value, "contentEncoding", "ContentEncoding", indent);
        EmitStringProperty(source, value, "contentMediaType", "ContentMediaType", indent);
        EmitNumericStringProperty(source, value, "minimum", "Minimum", indent);
        EmitNumericStringProperty(source, value, "maximum", "Maximum", indent);
        EmitNumericStringProperty(source, value, "exclusiveMinimum", "ExclusiveMinimum", indent);
        EmitNumericStringProperty(source, value, "exclusiveMaximum", "ExclusiveMaximum", indent);
        EmitNumericStringProperty(source, value, "multipleOf", "MultipleOf", indent);
        EmitIntegerProperty(source, value, "minLength", "MinLength", indent);
        EmitIntegerProperty(source, value, "maxLength", "MaxLength", indent);
        EmitIntegerProperty(source, value, "minItems", "MinItems", indent);
        EmitIntegerProperty(source, value, "maxItems", "MaxItems", indent);
        EmitIntegerProperty(source, value, "minProperties", "MinProperties", indent);
        EmitIntegerProperty(source, value, "maxProperties", "MaxProperties", indent);
        EmitIntegerProperty(source, value, "minContains", "MinContains", indent);
        EmitIntegerProperty(source, value, "maxContains", "MaxContains", indent);
        EmitBooleanProperty(source, value, "uniqueItems", "UniqueItems", indent);
        EmitBooleanProperty(source, value, "readOnly", "ReadOnly", indent);
        EmitBooleanProperty(source, value, "writeOnly", "WriteOnly", indent);
        if (value.Properties.TryGetValue("properties", out var properties))
        {
            source.Append(' ', (indent + 1) * 4)
                .AppendLine("Properties = new global::System.Collections.Generic.Dictionary<string, global::Microsoft.OpenApi.IOpenApiSchema>(global::System.StringComparer.Ordinal)")
                .Append(' ', (indent + 1) * 4).AppendLine("{");
            foreach (var property in ((JsonObject)properties).Properties)
            {
                source.Append(' ', (indent + 2) * 4).Append("[\"")
                    .Append(Escape(property.Key)).AppendLine("\"] =");
                EmitSchema(source, property.Value, new string(' ', (indent + 3) * 4), indent + 3);
            }
            source.Append(' ', (indent + 1) * 4).AppendLine("},");
        }
        EmitSchemaMap(source, value, "$defs", "Definitions", indent);
        EmitSchemaMap(source, value, "patternProperties", "PatternProperties", indent);
        EmitSchemaMap(source, value, "dependentSchemas", "DependentSchemas", indent);
        EmitDependentRequired(source, value, indent);
        if (value.Properties.TryGetValue("required", out var required))
        {
            source.Append(' ', (indent + 1) * 4)
                .AppendLine("Required = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal)")
                .Append(' ', (indent + 1) * 4).AppendLine("{");
            foreach (var item in ((JsonArray)required).Items.Cast<JsonString>())
            {
                source.Append(' ', (indent + 2) * 4).Append('"').Append(Escape(item.Value)).AppendLine("\",");
            }
            source.Append(' ', (indent + 1) * 4).AppendLine("},");
        }
        EmitChildSchema(source, value, "items", "Items", indent);
        EmitChildSchema(source, value, "additionalProperties", "AdditionalProperties", indent);
        EmitChildSchema(source, value, "not", "Not", indent);
        EmitChildSchema(source, value, "contains", "Contains", indent);
        EmitChildSchema(source, value, "propertyNames", "PropertyNames", indent);
        EmitChildSchema(source, value, "if", "If", indent);
        EmitChildSchema(source, value, "then", "Then", indent);
        EmitChildSchema(source, value, "else", "Else", indent);
        EmitChildSchema(source, value, "contentSchema", "ContentSchema", indent);
        EmitChildSchema(source, value, "unevaluatedProperties", "UnevaluatedPropertiesSchema", indent);
        EmitAllOf(source, value, indent);
        EmitSchemaList(source, value, "anyOf", "AnyOf", indent);
        EmitSchemaList(source, value, "oneOf", "OneOf", indent);
        if (value.Properties.TryGetValue("prefixItems", out var prefixItems))
        {
            source.Append(' ', (indent + 1) * 4)
                .AppendLine("Metadata = openApiVersion == global::Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0")
                .AppendLine("    ? null")
                .AppendLine("    : new global::System.Collections.Generic.Dictionary<string, object>")
                .Append(' ', (indent + 1) * 4).AppendLine("{")
                .Append(' ', (indent + 2) * 4)
                .AppendLine("[\"x-schema-prefix-items\"] = new global::Microsoft.OpenApi.IOpenApiSchema[]")
                .Append(' ', (indent + 2) * 4).AppendLine("{");
            foreach (var prefixItem in ((JsonArray)prefixItems).Items)
            {
                EmitSchema(source, prefixItem, new string(' ', (indent + 3) * 4), indent + 3);
            }
            source.Append(' ', (indent + 2) * 4).AppendLine("},")
                .Append(' ', (indent + 1) * 4).AppendLine("},");
            if (!value.Properties.ContainsKey("items"))
            {
                source.Append(' ', (indent + 1) * 4)
                    .AppendLine("Items = openApiVersion == global::Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0")
                    .AppendLine("    ? new global::Microsoft.OpenApi.OpenApiSchema()")
                    .AppendLine("    : null,");
            }
        }
        source.Append(' ', indent * 4).Append('}').AppendLine(terminator);
    }

    private static void EmitSchemaMap(
        StringBuilder source,
        JsonObject parent,
        string keyword,
        string property,
        int indent)
    {
        if (!parent.Properties.TryGetValue(keyword, out var schemas))
        {
            return;
        }
        source.Append(' ', (indent + 1) * 4).Append(property)
            .AppendLine(" = new global::System.Collections.Generic.Dictionary<string, global::Microsoft.OpenApi.IOpenApiSchema>(global::System.StringComparer.Ordinal)")
            .Append(' ', (indent + 1) * 4).AppendLine("{");
        foreach (var schema in ((JsonObject)schemas).Properties)
        {
            source.Append(' ', (indent + 2) * 4).Append("[\"")
                .Append(Escape(schema.Key)).AppendLine("\"] =");
            EmitSchema(source, schema.Value, new string(' ', (indent + 3) * 4), indent + 3);
        }
        source.Append(' ', (indent + 1) * 4).AppendLine("},");
    }

    private static void EmitAllOf(StringBuilder source, JsonObject parent, int indent)
    {
        var hasReference = parent.Properties.TryGetValue("$ref", out var reference);
        var hasAllOf = parent.Properties.TryGetValue("allOf", out var allOf);
        if (!hasReference && !hasAllOf)
        {
            return;
        }
        source.Append(' ', (indent + 1) * 4)
            .AppendLine("AllOf = new global::System.Collections.Generic.List<global::Microsoft.OpenApi.IOpenApiSchema>")
            .Append(' ', (indent + 1) * 4).AppendLine("{");
        if (reference is JsonString referenceString)
        {
            source.Append(' ', (indent + 2) * 4)
                .AppendLine("openApiVersion == global::Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0")
                .Append(' ', (indent + 3) * 4)
                .AppendLine("? new global::Microsoft.OpenApi.OpenApiSchema()")
                .Append(' ', (indent + 3) * 4)
                .Append(": new global::Microsoft.OpenApi.OpenApiSchemaReference(\"")
                .Append(Escape(referenceString.Value)).AppendLine("\"),");
        }
        if (allOf is JsonArray schemas)
        {
            foreach (var schema in schemas.Items)
            {
                EmitSchema(source, schema, new string(' ', (indent + 2) * 4), indent + 2);
            }
        }
        source.Append(' ', (indent + 1) * 4).AppendLine("},");
    }

    private static void EmitChildSchema(
        StringBuilder source,
        JsonObject parent,
        string keyword,
        string property,
        int indent)
    {
        if (!parent.Properties.TryGetValue(keyword, out var child))
        {
            return;
        }
        if (child is JsonBoolean boolean && keyword == "additionalProperties")
        {
            source.Append(' ', (indent + 1) * 4).Append("AdditionalPropertiesAllowed = ")
                .Append(boolean.Value ? "true" : "false").AppendLine(",");
            return;
        }
        source.Append(' ', (indent + 1) * 4).Append(property).AppendLine(" =");
        EmitSchema(source, child, new string(' ', (indent + 2) * 4), indent + 2);
    }

    private static void EmitSchemaList(
        StringBuilder source,
        JsonObject parent,
        string keyword,
        string property,
        int indent)
    {
        if (!parent.Properties.TryGetValue(keyword, out var schemas))
        {
            return;
        }
        source.Append(' ', (indent + 1) * 4).Append(property)
            .AppendLine(" = new global::System.Collections.Generic.List<global::Microsoft.OpenApi.IOpenApiSchema>")
            .Append(' ', (indent + 1) * 4).AppendLine("{");
        foreach (var schema in ((JsonArray)schemas).Items)
        {
            EmitSchema(source, schema, new string(' ', (indent + 2) * 4), indent + 2);
        }
        source.Append(' ', (indent + 1) * 4).AppendLine("},");
    }

    private static void EmitStringProperty(
        StringBuilder source,
        JsonObject parent,
        string keyword,
        string property,
        int indent)
    {
        if (parent.Properties.TryGetValue(keyword, out var value) && value is JsonString text)
        {
            source.Append(' ', (indent + 1) * 4).Append(property).Append(" = \"")
                .Append(Escape(text.Value)).AppendLine("\",");
        }
    }

    private static void EmitNumericStringProperty(
        StringBuilder source,
        JsonObject parent,
        string keyword,
        string property,
        int indent)
    {
        if (parent.Properties.TryGetValue(keyword, out var value) && value is JsonNumber number)
        {
            source.Append(' ', (indent + 1) * 4).Append(property).Append(" = \"")
                .Append(number.Value).AppendLine("\",");
        }
    }

    private static void EmitIntegerProperty(
        StringBuilder source,
        JsonObject parent,
        string keyword,
        string property,
        int indent)
    {
        if (parent.Properties.TryGetValue(keyword, out var value) && value is JsonNumber number)
        {
            source.Append(' ', (indent + 1) * 4).Append(property).Append(" = ")
                .Append(number.Value).AppendLine(",");
        }
    }

    private static void EmitBooleanProperty(
        StringBuilder source,
        JsonObject parent,
        string keyword,
        string property,
        int indent)
    {
        if (parent.Properties.TryGetValue(keyword, out var value) && value is JsonBoolean boolean)
        {
            source.Append(' ', (indent + 1) * 4).Append(property).Append(" = ")
                .Append(boolean.Value ? "true" : "false").AppendLine(",");
        }
    }

    private static void EmitDependentRequired(
        StringBuilder source,
        JsonObject parent,
        int indent)
    {
        if (!parent.Properties.TryGetValue("dependentRequired", out var dependentRequired))
        {
            return;
        }

        source.Append(' ', (indent + 1) * 4)
            .AppendLine("DependentRequired = new global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.HashSet<string>>(global::System.StringComparer.Ordinal)")
            .Append(' ', (indent + 1) * 4).AppendLine("{");
        foreach (var dependency in ((JsonObject)dependentRequired).Properties)
        {
            source.Append(' ', (indent + 2) * 4).Append("[\"")
                .Append(Escape(dependency.Key))
                .AppendLine("\"] = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal)")
                .Append(' ', (indent + 2) * 4).AppendLine("{");
            foreach (var item in ((JsonArray)dependency.Value).Items.Cast<JsonString>())
            {
                source.Append(' ', (indent + 3) * 4).Append('"')
                    .Append(Escape(item.Value)).AppendLine("\",");
            }
            source.Append(' ', (indent + 2) * 4).AppendLine("},");
        }
        source.Append(' ', (indent + 1) * 4).AppendLine("},");
    }

    private static string EmitType(JsonValue value)
    {
        var values = value is JsonArray array
            ? array.Items.Cast<JsonString>().Select(static item => item.Value)
            : [((JsonString)value).Value];
        return string.Join(" | ", values.Select(static type => type switch
        {
            "null" => "global::Microsoft.OpenApi.JsonSchemaType.Null",
            "boolean" => "global::Microsoft.OpenApi.JsonSchemaType.Boolean",
            "object" => "global::Microsoft.OpenApi.JsonSchemaType.Object",
            "array" => "global::Microsoft.OpenApi.JsonSchemaType.Array",
            "number" => "global::Microsoft.OpenApi.JsonSchemaType.Number",
            "string" => "global::Microsoft.OpenApi.JsonSchemaType.String",
            "integer" => "global::Microsoft.OpenApi.JsonSchemaType.Integer",
            _ => throw new InvalidOperationException(),
        }));
    }

    private static void EmitByteArray(StringBuilder source, string name, byte[] bytes)
    {
        source.Append("    private static readonly byte[] ").Append(name).AppendLine(" =")
            .AppendLine("    [");
        for (var index = 0; index < bytes.Length; index += 24)
        {
            source.Append("        ");
            for (var offset = index; offset < Math.Min(index + 24, bytes.Length); offset++)
            {
                if (offset > index)
                {
                    source.Append(' ');
                }
                source.Append(bytes[offset].ToString(CultureInfo.InvariantCulture)).Append(',');
            }
            source.AppendLine();
        }
        source.AppendLine("    ];");
    }

    private static string CreateReferencesJson(IReadOnlyList<string> references)
        => $"[{string.Join(",", references.Select(static reference => $"\"{Escape(reference)}\""))}]";

    private static string SanitizeIdentifier(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character == '_' || char.IsLetterOrDigit(character))
            {
                result.Append(character);
            }
        }
        if (result.Length != 0 && char.IsDigit(result[0]))
        {
            result.Insert(0, '_');
        }
        return result.ToString();
    }

    private static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string Hash(byte[] data)
    {
        using var algorithm = SHA256.Create();
        return ToHex(algorithm.ComputeHash(data));
    }

    private static string CompositeHash(
        byte[] schema,
        int dialect,
        int capabilities,
        string configurationIdentity)
    {
        var semantics = new byte[8];
        WriteInt32(semantics, 0, dialect);
        WriteInt32(semantics, 4, capabilities);
        var configuration = Encoding.UTF8.GetBytes(configurationIdentity);
        var value = new byte[schema.Length + semantics.Length + configuration.Length];
        Buffer.BlockCopy(schema, 0, value, 0, schema.Length);
        Buffer.BlockCopy(semantics, 0, value, schema.Length, semantics.Length);
        Buffer.BlockCopy(configuration, 0, value, schema.Length + semantics.Length, configuration.Length);
        return Hash(value);
    }

    private static void WriteInt32(byte[] target, int offset, int value)
    {
        target[offset] = (byte)value;
        target[offset + 1] = (byte)(value >> 8);
        target[offset + 2] = (byte)(value >> 16);
        target[offset + 3] = (byte)(value >> 24);
    }

    private static string ToHex(byte[] value)
    {
        var result = new char[value.Length * 2];
        const string Hex = "0123456789abcdef";
        for (var index = 0; index < value.Length; index++)
        {
            result[index * 2] = Hex[value[index] >> 4];
            result[index * 2 + 1] = Hex[value[index] & 0xf];
        }
        return new string(result);
    }

    private sealed record SchemaInput(
        string FileName,
        string? Source,
        string? LogicalName,
        string? Dialect,
        string? Capabilities,
        string? SchemaIdentity,
        string? ClrType,
        string? ValidatorType,
        string? ValidatorConfigurationIdentity);
}
