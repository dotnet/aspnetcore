// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl
{
    internal class OutputFlowControl
    {
        private FlowControl _flow;
        private readonly AwaitableProvider _awaitableProvider;

        public OutputFlowControl(AwaitableProvider awaitableProvider, uint initialWindowSize)
        {
            _flow = new FlowControl(initialWindowSize);
            _awaitableProvider = awaitableProvider;
        }

        public int Available => _flow.Available;
        public bool IsAborted => _flow.IsAborted;

        public ManualResetValueTaskSource<object> AvailabilityAwaitable
        {
            get
            {
                Debug.Assert(!_flow.IsAborted, $"({nameof(AvailabilityAwaitable)} accessed after abort.");
                Debug.Assert(_flow.Available <= 0, $"({nameof(AvailabilityAwaitable)} accessed with {Available} bytes available.");

                return _awaitableProvider.GetAwaitable();
            }
        }

        public void Reset(uint initialWindowSize)
        {
            // When output flow control is reused the client window size needs to be reset.
            // The client might have changed the window size before the stream is reused.
            _flow = new FlowControl(initialWindowSize);
            Debug.Assert(_awaitableProvider.ActiveCount == 0, "Queue should have been emptied by the previous stream.");
        }

        public void Advance(int bytes)
        {
            _flow.Advance(bytes);
        }

        // bytes can be negative when SETTINGS_INITIAL_WINDOW_SIZE decreases mid-connection.
        // This can also cause Available to become negative which MUST be allowed.
        // https://httpwg.org/specs/rfc7540.html#rfc.section.6.9.2
        public bool TryUpdateWindow(int bytes)
        {
            if (_flow.TryUpdateWindow(bytes))
            {
                while (_flow.Available > 0 && _awaitableProvider.ActiveCount > 0)
                {
                    _awaitableProvider.CompleteCurrent();
                }

                return true;
            }

            return false;
        }

        public void Abort()
        {
            // Make sure to set the aborted flag before running any continuations.
            _flow.Abort();

            while (_awaitableProvider.ActiveCount > 0)
            {
                _awaitableProvider.CompleteCurrent();
            }
        }
    }
}
