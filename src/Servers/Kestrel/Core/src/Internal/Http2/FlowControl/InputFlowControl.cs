// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

/// <summary>
/// Represents the in-bound flow control state of a stream or connection.
/// </summary>
/// <remarks>
/// Owns a <see cref="FlowControl"/> that it uses to track the present window size.
/// <para/>
/// <see cref="Http2Connection"/> owns an instance for the connection-level flow control.
/// <see cref="StreamInputFlowControl"/> owns an instance for the stream-level flow control.
/// <para/>
/// Reusable after calling <see cref="Reset"/>.
/// </remarks>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc9113#name-flow-control"/>
internal sealed class InputFlowControl
{
    private struct FlowControlState
    {
        private const long AbortedBitMask = 1L << 32; // uint MaxValue + 1
        internal long _state;

        public FlowControlState(uint initialWindowSize, bool isAborted)
        {
            _state = initialWindowSize;
            if (isAborted)
            {
                _state |= AbortedBitMask;
            }
        }

        public uint Available => (uint)_state;

        public bool IsAborted => _state > uint.MaxValue;
    }

    private readonly uint _initialWindowSize;
    private readonly int _minWindowSizeIncrement;

    private FlowControlState _flow;
    private int _pendingUpdateSize;
    private bool _windowUpdatesDisabled;

    public InputFlowControl(uint initialWindowSize, uint minWindowSizeIncrement)
    {
        Debug.Assert(initialWindowSize >= minWindowSizeIncrement, "minWindowSizeIncrement is greater than the window size.");

        _flow = new FlowControlState(initialWindowSize, false);
        _initialWindowSize = initialWindowSize;
        _minWindowSizeIncrement = (int)minWindowSizeIncrement;
    }

    public bool IsAvailabilityLow => _flow.Available < _minWindowSizeIncrement;

    // Test hook, not participating in mutual exclusion
    internal uint Available => _flow.Available;

    public void Reset()
    {
        _flow = new FlowControlState(_initialWindowSize, false);
        _pendingUpdateSize = 0;
        _windowUpdatesDisabled = false;
    }

    public bool TryAdvance(int bytes)
    {
        FlowControlState currentFlow, computedFlow;
        do
        {
            currentFlow = _flow; // Copy
            // Even if the stream is aborted, the client should never send more data than was available in the
            // flow-control window at the time of the abort.
            if (bytes > currentFlow.Available)
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorFlowControlWindowExceeded, Http2ErrorCode.FLOW_CONTROL_ERROR, ConnectionEndReason.FlowControlWindowExceeded);
            }

            if (currentFlow.IsAborted)
            {
                return false;
            }

            computedFlow = new FlowControlState(currentFlow.Available - (uint)bytes, isAborted: false);
        } while (currentFlow._state != Interlocked.CompareExchange(ref _flow._state, computedFlow._state, currentFlow._state));

        return true;
    }

    public bool TryUpdateWindow(int bytes, out int updateSize)
    {
        FlowControlState currentFlow, computedFlow;
        do
        {
            updateSize = 0;
            currentFlow = _flow; // Copy
            if (currentFlow.IsAborted)
            {
                // All data received by stream has already been returned to the connection window.
                return false;
            }

            var maxUpdate = int.MaxValue - currentFlow.Available;
            if (bytes > maxUpdate)
            {
                // We only try to update the window back to its initial size after the app consumes data.
                // It shouldn't be possible for the window size to ever exceed Http2PeerSettings.MaxWindowSize.
                Debug.Assert(false, $"{nameof(TryUpdateWindow)} attempted to grow window past max size.");
            }
            computedFlow = new FlowControlState(currentFlow.Available + (uint)bytes, isAborted: false);
        } while (currentFlow._state != Interlocked.CompareExchange(ref _flow._state, computedFlow._state, currentFlow._state));

        if (_windowUpdatesDisabled)
        {
            // Continue returning space to the connection window. The end of the stream has already
            // been received, so don't send window updates for the stream window.
            return true;
        }

        int computedPendingUpdateSize, currentPendingSize;
        do
        {
            updateSize = 0;
            currentPendingSize = _pendingUpdateSize;
            var potentialUpdateSize = currentPendingSize + bytes;
            if (potentialUpdateSize > _minWindowSizeIncrement)
            {
                computedPendingUpdateSize = 0;
                updateSize = potentialUpdateSize;
            }
            else
            {
                computedPendingUpdateSize = potentialUpdateSize;
            }
        } while (currentPendingSize != Interlocked.CompareExchange(ref _pendingUpdateSize, computedPendingUpdateSize, currentPendingSize));

        return true;
    }

    public void StopWindowUpdates()
    {
        _windowUpdatesDisabled = true;
    }

    public int Abort()
    {
        FlowControlState currentFlow, computedFlow;
        do
        {
            currentFlow = _flow; // Copy
            if (currentFlow.IsAborted)
            {
                return 0;
            }

            computedFlow = new FlowControlState(currentFlow.Available, isAborted: true);
        } while (currentFlow._state != Interlocked.CompareExchange(ref _flow._state, computedFlow._state, currentFlow._state));

        // Tell caller to return connection window space consumed by this stream. Even if window updates have
        // been disabled at the stream level, connection-level window updates may still be necessary.
        return (int)(_initialWindowSize - computedFlow.Available);
    }
}

