// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Provides a collection of <see cref="IClientModelValidator"/>s.
/// </summary>
public interface IClientModelValidatorProvider
{
    /// <summary>
    /// Creates set of <see cref="IClientModelValidator"/>s by updating
    /// <see cref="ClientValidatorItem.Validator"/> in <see cref="ClientValidatorProviderContext.Results"/>.
    /// </summary>
    /// <param name="context">The <see cref="ClientModelValidationContext"/> associated with this call.</param>
    void CreateValidators(ClientValidatorProviderContext context);
}
