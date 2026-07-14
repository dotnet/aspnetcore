// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Implemented by <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> subclasses
/// that support client-side validation by emitting <c>data-val-*</c> HTML attributes.
/// </summary>
public interface IClientValidationAdapter
{
    /// <summary>
    /// Produces the client-side validation rules for this attribute.
    /// Return an empty sequence if the attribute should not emit any client-side rule.
    /// </summary>
    /// <param name="errorMessage">
    /// The pre-formatted (optionally localized) error message.
    /// </param>
    IEnumerable<ClientValidationRule> GetClientValidationRules(string errorMessage);
}

