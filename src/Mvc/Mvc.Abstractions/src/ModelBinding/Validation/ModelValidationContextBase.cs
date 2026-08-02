// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// A common base class for <see cref="ModelValidationContext"/> and <see cref="ClientModelValidationContext"/>.
/// </summary>
public class ModelValidationContextBase
{
    /// <summary>
    /// Instantiates a new <see cref="ModelValidationContextBase"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/> for this context.</param>
    /// <param name="modelMetadata">The <see cref="ModelMetadata"/> for this model.</param>
    /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/> to be used by this context.</param>
    public ModelValidationContextBase(
        ActionContext actionContext,
        ModelMetadata modelMetadata,
        IModelMetadataProvider metadataProvider)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(modelMetadata);
        ArgumentNullException.ThrowIfNull(metadataProvider);

        ActionContext = actionContext;
        ModelMetadata = modelMetadata;
        MetadataProvider = metadataProvider;
    }

    /// <summary>
    /// Gets the <see cref="Mvc.ActionContext"/>.
    /// </summary>
    public ActionContext ActionContext { get; }

    /// <summary>
    /// Gets the <see cref="ModelBinding.ModelMetadata"/>.
    /// </summary>
    public ModelMetadata ModelMetadata { get; }

    /// <summary>
    /// Gets the <see cref="IModelMetadataProvider"/>.
    /// </summary>
    public IModelMetadataProvider MetadataProvider { get; }
}
