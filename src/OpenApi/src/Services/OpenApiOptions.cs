// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Options to support the construction of OpenAPI documents.
/// </summary>
public sealed class OpenApiOptions
{
    internal readonly List<IOpenApiDocumentTransformer> DocumentTransformers = [];
    internal readonly List<IOpenApiOperationTransformer> OperationTransformers = [];
    internal readonly List<IOpenApiSchemaTransformer> SchemaTransformers = [];

    /// <summary>
    /// A default implementation for creating a schema reference ID for a given <see cref="JsonTypeInfo"/>.
    /// </summary>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> associated with the schema we are generating a reference ID for.</param>
    /// <returns>The reference ID to use for the schema or <see langword="null"/> if the schema should always be inlined.</returns>
    public static string? CreateDefaultSchemaReferenceId(JsonTypeInfo jsonTypeInfo) => jsonTypeInfo.GetSchemaReferenceId();

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiOptions"/> class
    /// with the default <see cref="ShouldInclude"/> predicate.
    /// </summary>
    public OpenApiOptions()
    {
        ShouldInclude = (description) => description.GroupName == null || description.GroupName == DocumentName;
    }

    /// <summary>
    /// The version of the OpenAPI specification to use. Defaults to <see cref="OpenApiSpecVersion.OpenApi3_0"/>.
    /// </summary>
    public OpenApiSpecVersion OpenApiVersion { get; set; } = OpenApiSpecVersion.OpenApi3_0;

    /// <summary>
    /// The name of the OpenAPI document this <see cref="OpenApiOptions"/> instance is associated with.
    /// </summary>
    public string DocumentName { get; internal set; } = OpenApiConstants.DefaultDocumentName;

    /// <summary>
    /// A delegate to determine whether a given <see cref="ApiDescription"/> should be included in the given OpenAPI document.
    /// </summary>
    public Func<ApiDescription, bool> ShouldInclude { get; set; }

    /// <summary>
    /// A delegate to determine how reference IDs should be created for schemas associated with types in the given OpenAPI document.
    /// </summary>
    /// <remarks>
    /// The default implementation uses the <see cref="CreateDefaultSchemaReferenceId"/> method to generate reference IDs. When
    /// the provided delegate returns <see langword="null"/>, the schema associated with the <see cref="JsonTypeInfo"/> will always be inlined.
    /// </remarks>
    public Func<JsonTypeInfo, string?> CreateSchemaReferenceId { get; set; } = CreateDefaultSchemaReferenceId;

    /// <summary>
    /// Registers a new document transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <typeparam name="TTransformerType">The type of the <see cref="IOpenApiDocumentTransformer"/> to instantiate.</typeparam>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddDocumentTransformer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransformerType>()
        where TTransformerType : IOpenApiDocumentTransformer
    {
        DocumentTransformers.Add(new TypeBasedOpenApiDocumentTransformer(typeof(TTransformerType)));
        return this;
    }

    /// <summary>
    /// Registers a given instance of <see cref="IOpenApiDocumentTransformer"/> on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <param name="transformer">The <see cref="IOpenApiDocumentTransformer"/> instance to use.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddDocumentTransformer(IOpenApiDocumentTransformer transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);

        DocumentTransformers.Add(transformer);
        return this;
    }

    /// <summary>
    /// Registers a given delegate as a document transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <param name="transformer">The delegate representing the document transformer.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddDocumentTransformer(Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);

        DocumentTransformers.Add(new DelegateOpenApiDocumentTransformer(transformer));
        return this;
    }

    /// <summary>
    /// Registers a new operation transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <typeparam name="TTransformerType">The type of the <see cref="IOpenApiOperationTransformer"/> to instantiate.</typeparam>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddOperationTransformer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransformerType>()
        where TTransformerType : IOpenApiOperationTransformer
    {
        OperationTransformers.Add(new TypeBasedOpenApiOperationTransformer(typeof(TTransformerType)));
        return this;
    }

    /// <summary>
    /// Registers a given instance of <see cref="IOpenApiOperationTransformer"/> on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <param name="transformer">The <see cref="IOpenApiOperationTransformer"/> instance to use.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddOperationTransformer(IOpenApiOperationTransformer transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);

        OperationTransformers.Add(transformer);
        return this;
    }

    /// <summary>
    /// Registers a given delegate as an operation transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <param name="transformer">The delegate representing the operation transformer.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddOperationTransformer(Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);

        OperationTransformers.Add(new DelegateOpenApiOperationTransformer(transformer));
        return this;
    }

    /// <summary>
    /// Registers a new schema transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <typeparam name="TTransformerType">The type of the <see cref="IOpenApiSchemaTransformer"/> to instantiate.</typeparam>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddSchemaTransformer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransformerType>()
        where TTransformerType : IOpenApiSchemaTransformer
    {
        SchemaTransformers.Add(new TypeBasedOpenApiSchemaTransformer(typeof(TTransformerType)));
        return this;
    }

    /// <summary>
    /// Registers a given instance of <see cref="IOpenApiOperationTransformer"/> on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <param name="transformer">The <see cref="IOpenApiOperationTransformer"/> instance to use.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddSchemaTransformer(IOpenApiSchemaTransformer transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);

        SchemaTransformers.Add(transformer);
        return this;
    }

    /// <summary>
    /// Registers a given delegate as a schema transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <param name="transformer">The delegate representing the schema transformer.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions AddSchemaTransformer(Func<OpenApiSchema, OpenApiSchemaTransformerContext, CancellationToken, Task> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);

        SchemaTransformers.Add(new DelegateOpenApiSchemaTransformer(transformer));
        return this;
    }
}
