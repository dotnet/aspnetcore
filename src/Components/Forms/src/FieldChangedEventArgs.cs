// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides information about the <see cref="EditContext.OnFieldChanged"/> event.
/// </summary>
public sealed class FieldChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="FieldChangedEventArgs"/>.
    /// </summary>
    /// <param name="fieldIdentifier">The <see cref="Forms.FieldIdentifier"/></param>
    public FieldChangedEventArgs(in FieldIdentifier fieldIdentifier)
    {
        FieldIdentifier = fieldIdentifier;
    }

    /// <summary>
    /// Identifies the field whose value has changed.
    /// </summary>
    public FieldIdentifier FieldIdentifier { get; }
}
