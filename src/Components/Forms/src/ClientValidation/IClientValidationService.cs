// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Provides client-side validation HTML attributes (<c>data-val-*</c>) for form fields
/// based on their <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/>s.
/// </summary>
public interface IClientValidationService
{
    /// <summary>
    /// Gets the <c>data-val-*</c> HTML attributes for a form field.
    /// Returns <see langword="null"/> if no validation attributes apply.
    /// </summary>
    IReadOnlyDictionary<string, object>? GetClientValidationAttributes(FieldIdentifier fieldIdentifier);
}
