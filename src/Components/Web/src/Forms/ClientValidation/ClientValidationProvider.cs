// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Provides client-side validation metadata for a form's model.
/// </summary>
public abstract class ClientValidationProvider
{
    /// <summary>
    /// Returns the descriptor describing client-side validation for the form's model,
    /// or <see langword="null"/> when no client-side validation data applies
    /// (interactive render modes, model not registered with the validation infrastructure, etc.).
    /// </summary>
    public abstract ClientValidationFormDescriptor? GetFormDescriptor(EditContext editContext);
}
