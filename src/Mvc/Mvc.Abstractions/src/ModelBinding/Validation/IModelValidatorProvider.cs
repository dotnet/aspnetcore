// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Provides validators for a model value.
/// </summary>
public interface IModelValidatorProvider
{
    /// <summary>
    /// Creates the validators for <see cref="ModelValidatorProviderContext.ModelMetadata"/>.
    /// </summary>
    /// <param name="context">The <see cref="ModelValidatorProviderContext"/>.</param>
    /// <remarks>
    /// Implementations should add the <see cref="IModelValidator"/> instances to the appropriate
    /// <see cref="ValidatorItem"/> instance which should be added to
    /// <see cref="ModelValidatorProviderContext.Results"/>.
    /// </remarks>
    void CreateValidators(ModelValidatorProviderContext context);
}
