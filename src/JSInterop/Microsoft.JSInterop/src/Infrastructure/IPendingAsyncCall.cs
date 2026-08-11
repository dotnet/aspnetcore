// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// A .NET to JS call that has been dispatched and is waiting for its result.
/// </summary>
/// <remarks>
/// The table of pending calls is keyed by an identifier that comes back over the wire, so the result
/// type is not available at the point the result arrives. Holding the pending call behind this
/// interface keeps the generic argument alive in the implementing type, so completing a call is a
/// virtual call into a body where the result type is a real type argument rather than a
/// <see cref="Type"/> that would have to be closed over at run time.
/// </remarks>
internal interface IPendingAsyncCall
{
    /// <summary>
    /// Deserializes the result from <paramref name="reader"/> and completes the call.
    /// </summary>
    /// <remarks>
    /// The runtime is passed in because the byte arrays pending revival have to be cleared after the
    /// result is read but before the call completes. Completing it runs the caller's continuation
    /// synchronously, and that continuation is allowed to start another call.
    /// </remarks>
    void Complete(JSRuntime runtime, ref Utf8JsonReader reader);

    /// <summary>
    /// Faults the call with <paramref name="exception"/>.
    /// </summary>
    void Fail(Exception exception);

    /// <summary>
    /// Cancels the call.
    /// </summary>
    void Cancel(CancellationToken cancellationToken);
}
