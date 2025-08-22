// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

/// <summary>
/// Represents the flow control window for an <see cref="InputFlowControl"/>.
/// Mutated as the available quota changes.
/// </summary>
/// <remarks>
/// "FlowControlWindow" would probably be a clearer name.
/// </remarks>
internal struct FlowControl
{
    public FlowControl(uint initialWindowSize)
    {
        Debug.Assert(initialWindowSize <= Http2PeerSettings.MaxWindowSize, $"{nameof(initialWindowSize)} too large.");

        Available = (int)initialWindowSize;
        IsAborted = false;
    }

    public int Available { readonly get; private set; }
    public bool IsAborted { readonly get; private set; }

    public void Advance(int bytes)
    {
        Debug.Assert(!IsAborted, $"({nameof(Advance)} called after abort.");
        Debug.Assert(bytes == 0 || (bytes > 0 && bytes <= Available), $"{nameof(Advance)}({bytes}) called with {Available} bytes available.");

        Available -= bytes;
    }

    // bytes can be negative when SETTINGS_INITIAL_WINDOW_SIZE decreases mid-connection.
    // This can also cause Available to become negative which MUST be allowed.
    // https://httpwg.org/specs/rfc7540.html#rfc.section.6.9.2
    public bool TryUpdateWindow(int bytes)
    {
        var maxUpdate = Http2PeerSettings.MaxWindowSize - Available;

        if (bytes > maxUpdate)
        {
            return false;
        }

        Available += bytes;

        return true;
    }

    public void Abort()
    {
        IsAborted = true;
    }
}
