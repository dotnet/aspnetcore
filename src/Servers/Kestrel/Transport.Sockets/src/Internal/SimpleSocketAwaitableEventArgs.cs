// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

internal sealed class SimpleSocketAwaitableEventArgs : SocketAsyncEventArgs, IDisposable, IValueTaskSource<int>
{
    static SimpleSocketAwaitableEventArgs()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var accept = Volatile.Read(ref _accepted);
                var hit = Volatile.Read(ref _hit);
                var miss = Volatile.Read(ref _miss);
                var start = Volatile.Read(ref _start);
                Console.WriteLine($"accept: {accept}, hit: {hit}, miss: {miss}, start: {start}");
                try
                {
                    foreach (var pair in _sources)
                    {
                        Console.WriteLine($"> {pair.Key}: {pair.Value.Count}");
                    }
                }
                catch { }
            }
        });
    }
    static int _hit, _miss, _start, _accepted;

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

    [Conditional("DEBUG")]
    public static void WriteDebug(string message) => Debug.WriteLine(message);

    private ValueTask<int> Result(bool isAsync) => isAsync ? new(this, _source.Version) : SyncResultInt32();

    private ValueTask<int> SyncResultInt32()
    {
        var err = SocketError;
        if (err == 0)
        {
            WriteDebug($"{LastOperation} (sync): {BytesTransferred}");
            return new(BytesTransferred); // bypass the abstraction

        }
        else
        {
            WriteDebug($"{LastOperation} (sync): {SocketError}");
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
            WriteDebug($"{LastOperation} (async): {BytesTransferred}");
            _source.SetResult(BytesTransferred);
        }
        else
        {
            WriteDebug($"{LastOperation} (async): {SocketError}");
            SetFaulted(ref _source, err);
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SetFaulted(ref ManualResetValueTaskSourceCore<int> source, SocketError error)
        => source.SetException(new SocketException((int)error));

    private static readonly ConcurrentQueue<Socket> _reusableSockets = new();

    const int MAX_TARGET_REUSABLE_SOCKETS = 64; // not exact
    public static Socket? TryGetSocket()
    {
        if (!_reusableSockets.TryDequeue(out var next))
        {
            Interlocked.Increment(ref _miss);
            return null;
        }
        Interlocked.Increment(ref _hit);
        return next;
    }

    public ValueTask RecycleSocketAsync(Socket socket)
    {
        if (socket is null || _reusableSockets.Count >= MAX_TARGET_REUSABLE_SOCKETS)
        {
            // nothing to do, or we have plenty of spare sockets
            // (note minor thread-race here, but we don't need to be exact)
            BestEffortsClose(socket);
            return default;
        }
        else
        {
            DisconnectReuseSocket = true;
            var pending = DisconnectAsync(socket);
            if (pending.IsCompletedSuccessfully)
            {
                _reusableSockets.Enqueue(socket);
                return default;
            }
            else
            {
                return ImplAsync(pending, socket);
            }
        }
        static async ValueTask ImplAsync(ValueTask<int> pending, Socket socket)
        {
            try
            {
                await pending;
                _reusableSockets.Enqueue(socket);
            }
            catch
            {
                // well, we tried
                BestEffortsClose(socket);
                return;
            }
        }
    }
    internal static void BestEffortsClose(Socket? socket)
    {
        if (socket is not null)
        {
            try { socket.Close(); } catch { }
            try { socket.Dispose(); } catch { }
        }
    }

    internal static Task SharedRecycleSocketAsync(Socket socket)
    {
        if (socket is null || _reusableSockets.Count >= MAX_TARGET_REUSABLE_SOCKETS)
        {
            BestEffortsClose(socket);
            return Task.CompletedTask;
        }

        bool dispose = true;
        var args = GetArgs(); // we need a SAEA to use disconnect
        try
        {
            var pending = args.RecycleSocketAsync(socket);
            if (pending.IsCompletedSuccessfully)
            {
                return Task.CompletedTask;
            }

            dispose = false; // defer to async impl
            return AsAsync(args, pending);
        }
        finally
        {
            if (dispose)
            {
                args.Dispose();
            }
        }

        static async Task AsAsync(SimpleSocketAwaitableEventArgs args, ValueTask pending)
        {
            try
            {
                await pending;
            }
            finally
            {
                args.Dispose();
            }
        }
    }

    internal static void OnStart(EndPoint? endPoint)
    {
        Interlocked.Increment(ref _start);
        if (endPoint is IPEndPoint ip)
        {
            var addr = ip.Address;
            if (!_sources.TryGetValue(addr, out var counter))
            {
                counter = new();
                if (!_sources.TryAdd(addr, counter))
                {
                    counter = _sources[addr];
                }
            }
            counter.Incr();
        }
    }

    internal static void OnAccepted() => Interlocked.Increment(ref _accepted);

    static readonly ConcurrentDictionary<IPAddress, Counter> _sources = new();

    class Counter
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public void Incr() => Interlocked.Increment(ref _count);
    }
}
