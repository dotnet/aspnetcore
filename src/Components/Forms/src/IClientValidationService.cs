// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Service that provides client-side validation HTML attributes for form fields.
/// When stored on <see cref="EditContext.Properties"/>, input components automatically
/// emit <c>data-val-*</c> attributes during rendering.
/// </summary>
public interface IClientValidationService
{
    /// <summary>
    /// Gets the <c>data-val-*</c> HTML attributes for the specified field.
    /// </summary>
    /// <param name="fieldIdentifier">The field to get validation attributes for.</param>
    /// <returns>
    /// A dictionary of HTML attribute name/value pairs (e.g., <c>data-val-required</c> → error message),
    /// or an empty dictionary if no client validation rules apply.
    /// </returns>
    IReadOnlyDictionary<string, string> GetValidationAttributes(FieldIdentifier fieldIdentifier);
}
