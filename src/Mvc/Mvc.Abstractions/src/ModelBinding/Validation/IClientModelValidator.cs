// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Specifies the contract for performing validation in the browser.
/// <para>
/// MVC's validation system invokes <see cref="IClientModelValidator"/> to gather attributes that apply to the
/// rendered HTML. The rendered view may have to reference JavaScript libraries, such as jQuery Unobtrusive Validation,
/// to provide client validation based on the presence of these attributes.
/// </para>
/// </summary>
public interface IClientModelValidator
{
    /// <summary>
    /// Called to add client-side model validation.
    /// </summary>
    /// <param name="context">The <see cref="ClientModelValidationContext"/>.</param>
    void AddValidation(ClientModelValidationContext context);
}
