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
    /// Gets a value indicating whether the current validation state is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ValidationStateChangedEventArgs" />
    /// </summary>
    public ValidationStateChangedEventArgs()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="ValidationStateChangedEventArgs"/> with the specified validation state.
    /// </summary>
    /// <param name="isValid">
    /// A value indicating whether the <see cref="EditContext"/> is valid at the time the event is raised.
    /// </param>
    public ValidationStateChangedEventArgs(bool isValid)
    {
        IsValid = isValid;
    }
}
