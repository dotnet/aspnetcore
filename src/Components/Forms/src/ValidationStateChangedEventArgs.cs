// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides information about the <see cref="EditContext.OnValidationStateChanged"/> event.
/// </summary>
public sealed class ValidationStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets a shared empty instance of <see cref="ValidationStateChangedEventArgs"/>.
    /// </summary>
    public static new readonly ValidationStateChangedEventArgs Empty = new ValidationStateChangedEventArgs();

    /// <summary>
    /// Creates a new instance of <see cref="ValidationStateChangedEventArgs" />.
    /// </summary>
    public ValidationStateChangedEventArgs()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="ValidationStateChangedEventArgs" /> with the specified field identifier.
    /// </summary>
    /// <param name="fieldIdentifier">The field whose validation state changed.</param>
    public ValidationStateChangedEventArgs(in FieldIdentifier fieldIdentifier)
    {
        FieldIdentifier = fieldIdentifier;
    }

    /// <summary>
    /// Gets the <see cref="Microsoft.AspNetCore.Components.Forms.FieldIdentifier"/> whose validation state changed, if available.
    /// </summary>
    /// <remarks>
    /// When this property is <c>null</c>, it indicates that the validation state change
    /// applies to the entire form rather than a specific field.
    /// </remarks>
    public FieldIdentifier? FieldIdentifier { get; }
}
