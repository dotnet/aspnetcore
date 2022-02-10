// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// The context for client-side model validation.
/// </summary>
public class ClientModelValidationContext : ModelValidationContextBase
{
    /// <summary>
    /// Create a new instance of <see cref="ClientModelValidationContext"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/> for validation.</param>
    /// <param name="metadata">The <see cref="ModelMetadata"/> for validation.</param>
    /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/> to be used in validation.</param>
    /// <param name="attributes">The attributes dictionary for the HTML tag being rendered.</param>
    public ClientModelValidationContext(
        ActionContext actionContext,
        ModelMetadata metadata,
        IModelMetadataProvider metadataProvider,
        IDictionary<string, string> attributes)
        : base(actionContext, metadata, metadataProvider)
    {
        Attributes = attributes;
    }

    /// <summary>
    /// Gets the attributes dictionary for the HTML tag being rendered.
    /// </summary>
    public IDictionary<string, string> Attributes { get; }
}
