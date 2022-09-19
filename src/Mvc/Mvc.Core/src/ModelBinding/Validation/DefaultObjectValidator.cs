// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// The default implementation of <see cref="IObjectModelValidator"/>.
/// </summary>
#pragma warning disable CA1852 // Seal internal types
internal class DefaultObjectValidator : ObjectModelValidator
#pragma warning restore CA1852 // Seal internal types
{
    private readonly MvcOptions _mvcOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
    /// </summary>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    /// <param name="validatorProviders">The list of <see cref="IModelValidatorProvider"/>.</param>
    /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
    public DefaultObjectValidator(
        IModelMetadataProvider modelMetadataProvider,
        IList<IModelValidatorProvider> validatorProviders,
        MvcOptions mvcOptions)
        : base(modelMetadataProvider, validatorProviders)
    {
        _mvcOptions = mvcOptions;
    }

    public override ValidationVisitor GetValidationVisitor(
        ActionContext actionContext,
        IModelValidatorProvider validatorProvider,
        ValidatorCache validatorCache,
        IModelMetadataProvider metadataProvider,
        ValidationStateDictionary? validationState)
    {
        var visitor = new ValidationVisitor(
            actionContext,
            validatorProvider,
            validatorCache,
            metadataProvider,
            validationState)
        {
            MaxValidationDepth = _mvcOptions.MaxValidationDepth,
            ValidateComplexTypesIfChildValidationFails = _mvcOptions.ValidateComplexTypesIfChildValidationFails,
        };

        return visitor;
    }
}
