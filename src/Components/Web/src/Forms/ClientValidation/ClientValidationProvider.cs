// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Provides client-side validation metadata for a form's model.
/// </summary>
public abstract class ClientValidationProvider
{
    /// <summary>
    /// Returns the descriptor describing client-side validation for the fields rendered in the
    /// form, or <see langword="null"/> when no client-side validation data applies (for example
    /// when none of the rendered fields are validated on the server).
    /// </summary>
    /// <param name="editContext">The <see cref="EditContext"/> of the form being rendered.</param>
    /// <param name="renderedFields">
    /// The fields an input was rendered for, keyed by <see cref="FieldIdentifier"/> with the
    /// rendered HTML <c>name</c> as the value. Implementations build client-side rules only for
    /// these fields.
    /// </param>
    /// <returns>
    /// A <see cref="ClientValidationFormDescriptor"/> describing the client-side validation rules,
    /// or <see langword="null"/> when there is nothing to emit.
    /// </returns>
    public abstract ClientValidationFormDescriptor? GetFormDescriptor(
        EditContext editContext,
        IReadOnlyDictionary<FieldIdentifier, string> renderedFields);
}
