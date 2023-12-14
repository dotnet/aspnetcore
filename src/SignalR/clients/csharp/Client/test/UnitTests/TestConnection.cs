// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

internal class TestConnection : ConnectionContext, IConnectionInherentKeepAliveFeature
{
    private readonly bool _autoHandshake;
    private readonly TaskCompletionSource _started = new TaskCompletionSource();
    private readonly TaskCompletionSource _disposed = new TaskCompletionSource();

    private int _disposeCount = 0;
    public Task Started => _started.Task;
    public Task Disposed => _disposed.Task;

    private readonly Func<Task> _onStart;
    private readonly Func<Task> _onDispose;
    private readonly bool _hasInherentKeepAlive;

    public override string ConnectionId { get; set; }

    public IDuplexPipe Application { get; }
    public override IDuplexPipe Transport { get; set; }

    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public int DisposeCount => _disposeCount;

    public override IDictionary<object, object> Items { get; set; } = new ConnectionItems();

    bool IConnectionInherentKeepAliveFeature.HasInherentKeepAlive => _hasInherentKeepAlive;

    public TestConnection(Func<Task> onStart = null, Func<Task> onDispose = null, bool autoHandshake = true, bool hasInherentKeepAlive = false, PipeOptions pipeOptions = null)
    {
        _autoHandshake = autoHandshake;
        _onStart = onStart ?? (() => Task.CompletedTask);
        _onDispose = onDispose ?? (() => Task.CompletedTask);
        _hasInherentKeepAlive = hasInherentKeepAlive;

        var options = pipeOptions ?? new PipeOptions(readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);

        var pair = DuplexPipe.CreateConnectionPair(options, options);
        Application = pair.Application;
        Transport = pair.Transport;

        Features.Set<IConnectionInherentKeepAliveFeature>(this);
    }

    public override ValueTask DisposeAsync() => DisposeCoreAsync();

    public async ValueTask<ConnectionContext> StartAsync()
    {
        _started.TrySetResult();

        await _onStart();

        if (_autoHandshake)
        {
            // We can't await this as it will block StartAsync which will block
            // HubConnection.StartAsync which sends the Handshake in the first place!
            _ = ReadHandshakeAndSendResponseAsync();
        }

        return this;
    }

    public async Task<string> ReadHandshakeAndSendResponseAsync(int minorVersion = 0)
    {
        var s = await ReadSentTextMessageAsync();

        byte[] response;

        var output = MemoryBufferWriter.Get();
        try
        {
            HandshakeProtocol.WriteResponseMessage(HandshakeResponseMessage.Empty, output);
            response = output.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(output);
        }

        await Application.Output.WriteAsync(response);

        return s;
    }

    public Task ReceiveJsonMessage(object jsonObject)
    {
        var json = JsonConvert.SerializeObject(jsonObject, Formatting.None);
        var bytes = FormatMessageToArray(Encoding.UTF8.GetBytes(json));

        return Application.Output.WriteAsync(bytes).AsTask();
    }

    public Task ReceiveTextAsync(string rawText)
    {
        return ReceiveBytesAsync(Encoding.UTF8.GetBytes(rawText));
    }

    public Task ReceiveBytesAsync(byte[] bytes)
    {
        return Application.Output.WriteAsync(bytes).AsTask();
    }

    public async Task<string> ReadSentTextMessageAsync(bool ignorePings = true)
    {
        // Read a single text message from the Application Input pipe

        while (true)
        {
            var result = await ReadSentTextMessageAsyncInner();
            if (result == null)
            {
                return null;
            }

            var receivedMessageType = (int?)JObject.Parse(result)["type"];

            if (ignorePings && receivedMessageType == HubProtocolConstants.PingMessageType)
            {
                continue;
            }
            return result;
        }
    }

    private async Task<string> ReadSentTextMessageAsyncInner()
    {
        while (true)
        {
            var result = await Application.Input.ReadAsync();
            var buffer = result.Buffer;
            var consumed = buffer.Start;

            try
            {
                if (TextMessageParser.TryParseMessage(ref buffer, out var payload))
                {
                    consumed = buffer.Start;
                    return Encoding.UTF8.GetString(payload.ToArray());
                }
                else if (result.IsCompleted)
                {
                    await Application.Output.CompleteAsync();
                    return null;
                }
            }
            finally
            {
                Application.Input.AdvanceTo(consumed);
            }
        }
    }

    public async Task<JObject> ReadSentJsonAsync()
    {
        return JObject.Parse(await ReadSentTextMessageAsync());
    }

    public async Task<IList<string>> ReadAllSentMessagesAsync(bool ignorePings = true)
    {
        if (!Disposed.IsCompleted)
        {
            throw new InvalidOperationException("The connection must be stopped before this method can be used.");
        }

        var results = new List<string>();

        while (true)
        {
            var message = await ReadSentTextMessageAsync(ignorePings);
            if (message == null)
            {
                break;
            }
            results.Add(message);
        }

        return results;
    }

    public void CompleteFromTransport(Exception ex = null)
    {
        Application.Output.Complete(ex);
    }

    private async ValueTask DisposeCoreAsync(Exception ex = null)
    {
        Interlocked.Increment(ref _disposeCount);
        _disposed.TrySetResult();
        await _onDispose();

        // Simulate HttpConnection's behavior by Completing the Transport pipe.
        Transport.Input.Complete();
        Transport.Output.Complete();
    }

    private byte[] FormatMessageToArray(byte[] message)
    {
        var output = new MemoryStream();
        output.Write(message, 0, message.Length);
        output.WriteByte(TextMessageFormatter.RecordSeparator);
        return output.ToArray();
    }
}

