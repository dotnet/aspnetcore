// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

internal sealed class SimpleSocketAwaitableEventArgs : SocketAsyncEventArgs, IDisposable, IValueTaskSource<int>
{
    private static readonly ConcurrentQueue<SimpleSocketAwaitableEventArgs> _args = new();

    public static bool SocketReuseEnabled { get; set; }

    private SimpleSocketAwaitableEventArgs() { }
    public static SimpleSocketAwaitableEventArgs GetArgs() => _args.TryDequeue(out var next) ? next : new();

    public new void Dispose()
    {
        if (_args.Count >= 32) // minor race here; doesn't need to be exact
        {
            base.Dispose();
        }
        else
        {
            SetBuffer(s_Empty); // better than leaking between calls
            UserToken = null;
            AcceptSocket = null;
            RemoteEndPoint = null;
            _args.Enqueue(this);
        }
    }

    private ManualResetValueTaskSourceCore<int> _source;

    public ValueTask<int> AcceptAsync(Socket server)
    {
        _source.Reset(); // expect async; need to reset ahead of time to avoid race
        return Result(server.AcceptAsync(this));
    }

    public ValueTask<int> DisconnectAsync(Socket socket)
    {
        _source.Reset(); // expect async; need to reset ahead of time to avoid race
        return Result(socket.DisconnectAsync(this));
    }

    private static readonly Memory<byte> s_Empty = default!;

    public ValueTask<int> ConnectAsync(Socket socket)
    {
        _source.Reset();
        return Result(socket.ConnectAsync(this));
    }

    public ValueTask<int> ReceiveAsync(Socket socket, byte[] buffer, int offset, int count)
    {
        SetBuffer(buffer, offset, count);
        return ReceiveAsync(socket);
    }

    public ValueTask<int> ReceiveAsync(Socket socket)
    {
        _source.Reset();
        return Result(socket.ReceiveAsync(this));
    }

    internal ValueTask<int> SendAsync(Socket client, byte[] buffer, int offset, int count)
    {
        SetBuffer(buffer, offset, count);
        return SendAsync(client);
    }

    public ValueTask<int> SendAsync(Socket socket)
    {
        _source.Reset();
        return Result(socket.SendAsync(this));
    }

    private ValueTask<int> Result(bool isAsync) => isAsync ? new(this, _source.Version) : SyncResultInt32();

    private ValueTask<int> SyncResultInt32()
    {
        var err = SocketError;
        if (err == 0)
        {
            return new(BytesTransferred); // bypass the abstraction

        }
        else
        {
            SetFaulted(ref _source, err);
            return new(this, _source.Version);
        }
    }

    int IValueTaskSource<int>.GetResult(short token) => _source.GetResult(token);

    ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token) => _source.GetStatus(token);

    void IValueTaskSource<int>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _source.OnCompleted(continuation, state, token, flags);

    protected override void OnCompleted(SocketAsyncEventArgs e)
    {
        var err = SocketError;
        if (err == 0)
        {
            _source.SetResult(BytesTransferred);
        }
        else
        {
            SetFaulted(ref _source, err);
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SetFaulted(ref ManualResetValueTaskSourceCore<int> source, SocketError error)
        => source.SetException(new SocketException((int)error));

    internal static void BestEffortsClose(Socket? socket)
    {
        if (socket is not null)
        {
            try { socket.Close(); } catch { }
            try { socket.Dispose(); } catch { }
        }
    }
}
