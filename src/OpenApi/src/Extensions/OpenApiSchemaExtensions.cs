// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiSchemaExtensions
{
    /// <summary>
    /// Generates a deep copy of a given <see cref="OpenApiSchema"/> instance.
    /// </summary>
    /// <remarks>
    /// The copy constructors of the <see cref="OpenApiSchema"/> class do not perform a deep
    /// copy of the instance which presents a problem whe making modifications in deeply nested
    /// subschemas. This extension implements a deep copy on <see cref="OpenApiSchema" /> to guarantee
    /// that modifications on cloned subschemas do not affect the original subschema.
    /// /// </remarks>
    /// <param name="schema">The <see cref="OpenApiSchema"/> to generate a deep copy of.</param>
    public static OpenApiSchema Clone(this OpenApiSchema schema)
    {
        return new OpenApiSchema
        {
            Title = schema.Title,
            Type = schema.Type,
            Format = schema.Format,
            Description = schema.Description,
            Maximum = schema.Maximum,
            ExclusiveMaximum = schema.ExclusiveMaximum,
            Minimum = schema.Minimum,
            ExclusiveMinimum = schema.ExclusiveMinimum,
            MaxLength = schema.MaxLength,
            MinLength = schema.MinLength,
            Pattern = schema.Pattern,
            MultipleOf = schema.MultipleOf,
            Default = OpenApiAnyCloneHelper.CloneFromCopyConstructor<IOpenApiAny>(schema.Default),
            ReadOnly = schema.ReadOnly,
            WriteOnly = schema.WriteOnly,
            AllOf = schema.AllOf != null ? new List<OpenApiSchema>(schema.AllOf.Select(s => s.Clone())) : null,
            OneOf = schema.OneOf != null ? new List<OpenApiSchema>(schema.OneOf.Select(s => s.Clone())) : null,
            AnyOf = schema.AnyOf != null ? new List<OpenApiSchema>(schema.AnyOf.Select(s => s.Clone())) : null,
            Not = schema.Not?.Clone(),
            Required = schema.Required != null ? new HashSet<string>(schema.Required) : null,
            Items = schema.Items?.Clone(),
            MaxItems = schema.MaxItems,
            MinItems = schema.MinItems,
            UniqueItems = schema.UniqueItems,
            Properties = schema.Properties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()),
            MaxProperties = schema.MaxProperties,
            MinProperties = schema.MinProperties,
            AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed,
            AdditionalProperties = schema.AdditionalProperties?.Clone(),
            Discriminator = schema.Discriminator != null ? new(schema.Discriminator) : null,
            Example = OpenApiAnyCloneHelper.CloneFromCopyConstructor<IOpenApiAny>(schema.Example),
            Enum = schema.Enum != null ? new List<IOpenApiAny>(schema.Enum) : null,
            Nullable = schema.Nullable,
            ExternalDocs = schema.ExternalDocs != null ? new(schema.ExternalDocs) : null,
            Deprecated = schema.Deprecated,
            Xml = schema.Xml != null ? new(schema.Xml) : null,
            Extensions = schema.Extensions != null ? new Dictionary<string, IOpenApiExtension>(schema.Extensions) : null,
            UnresolvedReference = schema.UnresolvedReference,
            Reference = schema.Reference != null ? new(schema.Reference) : null,
            Annotations = schema.Annotations != null ? new Dictionary<string, object>(schema.Annotations) : null,
        };
    }
}
