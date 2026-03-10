// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Defines a mapping from a validation attribute to
/// <c>data-val-*</c> HTML attributes for client-side validation.
/// The adapter is responsible only for emitting the correct attribute names
/// and parameter values; the error message is pre-resolved by the caller
/// and provided via the <c>errorMessage</c> parameter.
/// </summary>
public interface IClientValidationAdapter
{
    /// <summary>
    /// Adds <c>data-val-*</c> attributes to the <paramref name="context"/>'s
    /// attribute dictionary for the given validation attribute.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ClientValidationContext"/> containing the attribute
    /// dictionary.
    /// </param>
    /// <param name="errorMessage">
    /// The pre-resolved, fully formatted error message for this validation rule.
    /// </param>
    void AddClientValidation(in ClientValidationContext context, string errorMessage);
}
