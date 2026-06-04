// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Implemented by <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> subclasses
/// that contribute client-side validation rules to Blazor SSR forms.
/// </summary>
/// <remarks>
/// Attributes that implement this interface participate in the client-side validation pipeline
/// for forms rendered server-side. The framework collects the rules returned from
/// <see cref="GetClientValidationRules"/> across all participating attributes on the model and
/// serializes them into a <c>&lt;blazor-client-validation-data&gt;</c> element inside the form.
/// The shipped JS validation engine then enforces the rules in the browser before the form is
/// submitted. Rule names must match a validator registered on the JS side via
/// <c>Blazor.formValidation.addValidator(name, ...)</c>.
/// </remarks>
public interface IClientValidationAdapter
{
    /// <summary>
    /// Produces the client-side validation rules for this attribute. Return an empty sequence
    /// if the attribute should not emit any client-side rule for a particular invocation.
    /// </summary>
    /// <param name="errorMessage">
    /// The pre-formatted (and, when configured, localized) error message for the rule.
    /// </param>
    IEnumerable<ClientValidationRule> GetClientValidationRules(string errorMessage);
}
