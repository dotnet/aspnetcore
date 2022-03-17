// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides information about the <see cref="EditContext.OnValidationRequested"/> event.
/// </summary>
public sealed class ValidationRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets a shared empty instance of <see cref="ValidationRequestedEventArgs"/>.
    /// </summary>
    public static new readonly ValidationRequestedEventArgs Empty = new ValidationRequestedEventArgs();

    /// <summary>
    /// Creates a new instance of <see cref="ValidationRequestedEventArgs"/>.
    /// </summary>
    public ValidationRequestedEventArgs()
    {
    }
}
