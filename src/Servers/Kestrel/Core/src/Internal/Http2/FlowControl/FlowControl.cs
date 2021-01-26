// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl
{
    internal struct FlowControl
    {
        public FlowControl(uint initialWindowSize)
        {
            Debug.Assert(initialWindowSize <= Http2PeerSettings.MaxWindowSize, $"{nameof(initialWindowSize)} too large.");

            Available = (int)initialWindowSize;
            IsAborted = false;
        }

        public int Available { get; private set; }
        public bool IsAborted { get; private set; }

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
}
