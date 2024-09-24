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
    private readonly int _initialWindowSize;
    private readonly int _minWindowSizeIncrement;

    private FlowControl _flow;
    private int _pendingUpdateSize;
    private bool _windowUpdatesDisabled;
    private readonly Lock _flowLock = new();

    public InputFlowControl(uint initialWindowSize, uint minWindowSizeIncrement)
    {
        Debug.Assert(initialWindowSize >= minWindowSizeIncrement, "minWindowSizeIncrement is greater than the window size.");

        _flow = new FlowControl(initialWindowSize);
        _initialWindowSize = (int)initialWindowSize;
        _minWindowSizeIncrement = (int)minWindowSizeIncrement;
    }

    public bool IsAvailabilityLow => _flow.Available < _minWindowSizeIncrement;

    public void Reset()
    {
        _flow = new FlowControl((uint)_initialWindowSize);
        _pendingUpdateSize = 0;
        _windowUpdatesDisabled = false;
    }

    public bool TryAdvance(int bytes)
    {
        lock (_flowLock)
        {
            // Even if the stream is aborted, the client should never send more data than was available in the
            // flow-control window at the time of the abort.
            if (bytes > _flow.Available)
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorFlowControlWindowExceeded, Http2ErrorCode.FLOW_CONTROL_ERROR, ConnectionEndReason.FlowControlWindowExceeded);
            }

            if (_flow.IsAborted)
            {
                // This data won't be read by the app, so tell the caller to count the data as already consumed.
                return false;
            }

            _flow.Advance(bytes);
            return true;
        }
    }

    public bool TryUpdateWindow(int bytes, out int updateSize)
    {
        lock (_flowLock)
        {
            updateSize = 0;

            if (_flow.IsAborted)
            {
                // All data received by stream has already been returned to the connection window.
                return false;
            }

            if (!_flow.TryUpdateWindow(bytes))
            {
                // We only try to update the window back to its initial size after the app consumes data.
                // It shouldn't be possible for the window size to ever exceed Http2PeerSettings.MaxWindowSize.
                Debug.Assert(false, $"{nameof(TryUpdateWindow)} attempted to grow window past max size.");
            }

            if (_windowUpdatesDisabled)
            {
                // Continue returning space to the connection window. The end of the stream has already
                // been received, so don't send window updates for the stream window.
                return true;
            }

            var potentialUpdateSize = _pendingUpdateSize + bytes;

            if (potentialUpdateSize > _minWindowSizeIncrement)
            {
                _pendingUpdateSize = 0;
                updateSize = potentialUpdateSize;
            }
            else
            {
                _pendingUpdateSize = potentialUpdateSize;
            }

            return true;
        }
    }

    public void StopWindowUpdates()
    {
        _windowUpdatesDisabled = true;
    }

    public int Abort()
    {
        lock (_flowLock)
        {
            if (_flow.IsAborted)
            {
                return 0;
            }

            _flow.Abort();

            // Tell caller to return connection window space consumed by this stream. Even if window updates have
            // been disabled at the stream level, connection-level window updates may still be necessary.
            return _initialWindowSize - _flow.Available;
        }
    }
}
