// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

internal sealed class StreamInputFlowControl
{
    private readonly InputFlowControl _connectionLevelFlowControl;
    private readonly InputFlowControl _streamLevelFlowControl;

    private int StreamId => _stream.StreamId;
    private readonly Http2Stream _stream;
    private readonly Http2FrameWriter _frameWriter;

    public StreamInputFlowControl(
        Http2Stream stream,
        Http2FrameWriter frameWriter,
        InputFlowControl connectionLevelFlowControl,
        uint initialWindowSize,
        uint minWindowSizeIncrement)
    {
        _connectionLevelFlowControl = connectionLevelFlowControl;
        _streamLevelFlowControl = new InputFlowControl(initialWindowSize, minWindowSizeIncrement);
        _stream = stream;
        _frameWriter = frameWriter;
    }

    public void Reset()
    {
        _streamLevelFlowControl.Reset();
    }

    public void Advance(int bytes)
    {
        var connectionSuccess = _connectionLevelFlowControl.TryAdvance(bytes);

        Debug.Assert(connectionSuccess, "Connection-level input flow control should never be aborted.");

        if (!_streamLevelFlowControl.TryAdvance(bytes))
        {
            // The stream has already been aborted, so immediately count the bytes as read at the connection level.
            UpdateConnectionWindow(bytes);
        }
    }

    public void UpdateWindows(int bytes)
    {
        if (!_streamLevelFlowControl.TryUpdateWindow(bytes, out var streamWindowUpdateSize))
        {
            // Stream-level flow control was aborted. Any unread bytes have already been returned to the connection
            // flow-control window by Abort().
            return;
        }

        if (streamWindowUpdateSize > 0)
        {
            // Writing with the FrameWriter should only fail if given a canceled token, so just fire and forget.
            _ = _frameWriter.WriteWindowUpdateAsync(StreamId, streamWindowUpdateSize).Preserve();
        }

        UpdateConnectionWindow(bytes);
    }

    public void StopWindowUpdates()
    {
        _streamLevelFlowControl.StopWindowUpdates();
    }

    public void Abort()
    {
        var unreadBytes = _streamLevelFlowControl.Abort();

        if (unreadBytes > 0)
        {
            // We assume that the app won't read the remaining data from the request body pipe.
            // Even if the app does continue reading, _streamLevelFlowControl.TryUpdateWindow() will return false
            // from now on which prevents double counting.
            UpdateConnectionWindow(unreadBytes);
        }
    }

    private void UpdateConnectionWindow(int bytes)
    {
        var connectionSuccess = _connectionLevelFlowControl.TryUpdateWindow(bytes, out var connectionWindowUpdateSize);

        Debug.Assert(connectionSuccess, "Connection-level input flow control should never be aborted.");

        if (connectionWindowUpdateSize > 0)
        {
            // Writing with the FrameWriter should only fail if given a canceled token, so just fire and forget.
            _ = _frameWriter.WriteWindowUpdateAsync(0, connectionWindowUpdateSize).Preserve();
        }
    }
}
