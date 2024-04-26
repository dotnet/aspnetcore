// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Nodes;

namespace JsonSchemaMapper;

/// <summary>
/// Controls the behavior of the <see cref="JsonSchemaMapper"/> class.
/// </summary>
#if EXPOSE_JSON_SCHEMA_MAPPER
public
#else
internal
#endif
    class JsonSchemaMapperConfiguration
{
    /// <summary>
    /// Gets the default configuration object used by <see cref="JsonSchemaMapper"/>.
    /// </summary>
    public static JsonSchemaMapperConfiguration Default { get; } = new();

    private readonly int _maxDepth = 64;

    /// <summary>
    /// Determines whether schema references using JSON pointers should be generated for repeated complex types.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="true"/>. Should be left enabled if recursive types (e.g. trees, linked lists) are expected.
    /// </remarks>
    public bool AllowSchemaReferences { get; init; } = true;

    /// <summary>
    /// Determines whether the '$schema' property should be included in the root schema document.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool IncludeSchemaVersion { get; init; } = true;

    /// <summary>
    /// Determines whether the <see cref="DescriptionAttribute"/> should be resolved for types and properties.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool ResolveDescriptionAttributes { get; init; } = true;

    /// <summary>
    /// Determines the nullability behavior of reference types in the generated schema.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="ReferenceTypeNullability.Annotated"/>. Currently JsonSerializer
    /// doesn't recognize non-nullable reference types (https://github.com/dotnet/runtime/issues/1256)
    /// so the serializer will always treat them as nullable. Setting to <see cref="ReferenceTypeNullability.AlwaysNullable"/>
    /// improves accuracy of the generated schema with respect to the actual serialization behavior but can result in more noise.
    /// </remarks>
    public ReferenceTypeNullability ReferenceTypeNullability { get; init; } = ReferenceTypeNullability.Annotated;

    /// <summary>
    /// Determines whether properties bound to non-optional constructor parameters should be flagged as required.
    /// </summary>
    /// <remarks>
    /// Defaults to true. Current STJ treats all constructor parameters as optional
    /// (https://github.com/dotnet/runtime/issues/100075) so disabling this option
    /// will generate schemas that are more compatible with the actual serialization behavior.
    /// </remarks>
    public bool RequireConstructorParameters { get; init; } = true;

    /// <summary>
    /// Defines a callback that is invoked for every schema that is generated within the type graph.
    /// </summary>
    public Action<JsonSchemaGenerationContext, JsonObject>? OnSchemaGenerated { get; init; }

    /// <summary>
    /// Determines the maximum permitted depth when traversing the generated type graph.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 0.</exception>
    /// <remarks>
    /// Defaults to 64.
    /// </remarks>
    public int MaxDepth
    {
        get => _maxDepth;
        init
        {
            if (value < 0)
            {
                Throw();
                static void Throw() => throw new ArgumentOutOfRangeException(nameof(value));
            }

            _maxDepth = value;
        }
    }
}
