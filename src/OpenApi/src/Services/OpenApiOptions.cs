// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
    /// Registers a new document transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <typeparam name="TTransformerType">The type of the <see cref="IOpenApiDocumentTransformer"/> to instantiate.</typeparam>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions UseTransformer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTransformerType>()
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
    public OpenApiOptions UseTransformer(IOpenApiDocumentTransformer transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer, nameof(transformer));

        DocumentTransformers.Add(transformer);
        return this;
    }

    /// <summary>
    /// Registers a given delegate as a document transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <param name="transformer">The delegate representing the document transformer.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions UseTransformer(Func<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken, Task> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer, nameof(transformer));

        DocumentTransformers.Add(new DelegateOpenApiDocumentTransformer(transformer));
        return this;
    }

    /// <summary>
    /// Registers a given delegate as an operation transformer on the current <see cref="OpenApiOptions"/> instance.
    /// </summary>
    /// <param name="transformer">The delegate representing the operation transformer.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance for further customization.</returns>
    public OpenApiOptions UseOperationTransformer(Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer, nameof(transformer));

        DocumentTransformers.Add(new DelegateOpenApiDocumentTransformer(transformer));
        return this;
    }
}
