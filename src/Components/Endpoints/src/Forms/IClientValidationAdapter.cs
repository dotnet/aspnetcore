// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Implemented by <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> subclasses
/// that contribute client-side validation rules to Blazor SSR forms.
/// </summary>
/// <remarks>
/// Attributes that implement this interface participate in the client-side validation pipeline
/// for forms rendered server-side. Rule names must match a validator registered on the JS side via
/// <c>Blazor.formValidation.addValidator(name, ...)</c>.
/// </remarks>
public interface IClientValidationAdapter
{
    /// <summary>
    /// Produces the client-side validation rules for this attribute.
    /// </summary>
    IEnumerable<ClientValidationRule> GetClientValidationRules();
}
