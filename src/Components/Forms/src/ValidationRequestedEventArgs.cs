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

    /// <summary>
    /// Creates a new instance of <see cref="ValidationRequestedEventArgs"/> with the specified
    /// <see cref="System.Threading.CancellationToken"/>.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when the caller has requested
    /// cancellation of the in-flight async validation pass.</param>
    public ValidationRequestedEventArgs(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets a token that signals when the caller has requested cancellation of the in-flight
    /// async validation pass. Synchronous handlers can ignore this; async handlers that perform
    /// long-running work (database lookups, remote API calls) should pass it to their downstream
    /// APIs so the work can be aborted promptly.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
