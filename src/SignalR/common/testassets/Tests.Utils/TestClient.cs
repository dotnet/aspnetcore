// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Tests
{
#if TESTUTILS
    public
#else
    internal
#endif
    class TestClient : ITransferFormatFeature, IConnectionHeartbeatFeature, IDisposable
    {
        private readonly object _heartbeatLock = new object();
        private List<(Action<object> handler, object state)> _heartbeatHandlers;

        private static int _id;
        private IHubProtocol _protocol;
        private readonly IInvocationBinder _invocationBinder;
        private readonly CancellationTokenSource _cts;

        public DefaultConnectionContext Connection { get; }
        public Task Connected => ((TaskCompletionSource<bool>)Connection.Items["ConnectedTask"]).Task;
        public HandshakeResponseMessage HandshakeResponseMessage { get; private set; }

        public TransferFormat SupportedFormats { get; set; } = TransferFormat.Text | TransferFormat.Binary;

        public TransferFormat ActiveFormat { get; set; }

        public TestClient(IHubProtocol protocol = null, IInvocationBinder invocationBinder = null, string userIdentifier = null, long pauseWriterThreshold = 32768)
        {
            var options = new PipeOptions(readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false,
                pauseWriterThreshold: pauseWriterThreshold, resumeWriterThreshold: pauseWriterThreshold / 2);
            var pair = DuplexPipe.CreateConnectionPair(options, options);
            Connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Transport, pair.Application);

            // Add features SignalR needs for testing
            Connection.Features.Set<ITransferFormatFeature>(this);
            Connection.Features.Set<IConnectionHeartbeatFeature>(this);

            var claimValue = Interlocked.Increment(ref _id).ToString();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, claimValue) };
            if (userIdentifier != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdentifier));
            }

            Connection.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            Connection.Items["ConnectedTask"] = new TaskCompletionSource<bool>();

            _protocol = protocol ?? new NewtonsoftJsonHubProtocol();
            _invocationBinder = invocationBinder ?? new DefaultInvocationBinder();

            _cts = new CancellationTokenSource();
        }

        public async Task<Task> ConnectAsync(
            Connections.ConnectionHandler handler,
            bool sendHandshakeRequestMessage = true,
            bool expectedHandshakeResponseMessage = true)
        {
            if (sendHandshakeRequestMessage)
            {
                await Connection.Application.Output.WriteAsync(GetHandshakeRequestMessage());
            }

            var connection = handler.OnConnectedAsync(Connection);

            if (expectedHandshakeResponseMessage)
            {
                // note that the handshake response might not immediately be readable
                // e.g. server is waiting for request, times out after configured duration,
                // and sends response with timeout error
                HandshakeResponseMessage = (HandshakeResponseMessage)await ReadAsync(true).OrTimeout();
            }

            return connection;
        }

        public Task<IList<HubMessage>> StreamAsync(string methodName, params object[] args)
        {
            return StreamAsync(methodName, streamIds: null, args);
        }

        public async Task<IList<HubMessage>> StreamAsync(string methodName, string[] streamIds, params object[] args)
        {
            var invocationId = await SendStreamInvocationAsync(methodName, streamIds, args);

            var messages = new List<HubMessage>();
            while (true)
            {
                var message = await ReadAsync();

                if (message == null)
                {
                    throw new InvalidOperationException("Connection aborted!");
                }

                if (message is HubInvocationMessage hubInvocationMessage && !string.Equals(hubInvocationMessage.InvocationId, invocationId))
                {
                    throw new NotSupportedException("TestClient does not support multiple outgoing invocations!");
                }

                switch (message)
                {
                    case StreamItemMessage _:
                        messages.Add(message);
                        break;
                    case CompletionMessage _:
                        messages.Add(message);
                        return messages;
                    default:
                        // Message implement ToString so this should be helpful.
                        throw new NotSupportedException($"TestClient recieved an unexpected message: {message}.");
                }
            }
        }

        public async Task<CompletionMessage> InvokeAsync(string methodName, params object[] args)
        {
            var invocationId = await SendInvocationAsync(methodName, nonBlocking: false, args: args);

            while (true)
            {
                var message = await ReadAsync();

                if (message == null)
                {
                    throw new InvalidOperationException("Connection aborted!");
                }

                if (message is HubInvocationMessage hubInvocationMessage && !string.Equals(hubInvocationMessage.InvocationId, invocationId))
                {
                    throw new NotSupportedException("TestClient does not support multiple outgoing invocations!");
                }

                switch (message)
                {
                    case StreamItemMessage result:
                        throw new NotSupportedException("Use 'StreamAsync' to call a streaming method");
                    case CompletionMessage completion:
                        return completion;
                    case PingMessage _:
                        // Pings are ignored
                        break;
                    default:
                        // Message implement ToString so this should be helpful.
                        throw new NotSupportedException($"TestClient recieved an unexpected message: {message}.");
                }
            }
        }

        public Task<string> SendInvocationAsync(string methodName, params object[] args)
        {
            return SendInvocationAsync(methodName, nonBlocking: false, args: args);
        }

        public Task<string> SendInvocationAsync(string methodName, bool nonBlocking, params object[] args)
        {
            var invocationId = nonBlocking ? null : GetInvocationId();
            return SendHubMessageAsync(new InvocationMessage(invocationId, methodName, args));
        }

        public Task<string> SendStreamInvocationAsync(string methodName, params object[] args)
        {
            return SendStreamInvocationAsync(methodName, streamIds: null, args);
        }

        public Task<string> SendStreamInvocationAsync(string methodName, string[] streamIds, params object[] args)
        {
            var invocationId = GetInvocationId();
            return SendHubMessageAsync(new StreamInvocationMessage(invocationId, methodName, args, streamIds));
        }

        public Task<string> BeginUploadStreamAsync(string invocationId, string methodName, string[] streamIds, params object[] args)
        {
            var message = new InvocationMessage(invocationId, methodName, args, streamIds);
            return SendHubMessageAsync(message);
        } 

        public async Task<string> SendHubMessageAsync(HubMessage message)
        {
            var payload = _protocol.GetMessageBytes(message);

            await Connection.Application.Output.WriteAsync(payload);
            return message is HubInvocationMessage hubMessage ? hubMessage.InvocationId : null;
        }

        public async Task<HubMessage> ReadAsync(bool isHandshake = false)
        {
            while (true)
            {
                var message = TryRead(isHandshake);

                if (message == null)
                {
                    var result = await Connection.Application.Input.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            continue;
                        }

                        if (result.IsCompleted)
                        {
                            return null;
                        }
                    }
                    finally
                    {
                        Connection.Application.Input.AdvanceTo(buffer.Start);
                    }
                }
                else
                {
                    return message;
                }
            }
        }

        public HubMessage TryRead(bool isHandshake = false)
        {
            if (!Connection.Application.Input.TryRead(out var result))
            {
                return null;
            }

            var buffer = result.Buffer;

            try
            {
                if (!isHandshake)
                {
                    if (_protocol.TryParseMessage(ref buffer, _invocationBinder, out var message))
                    {
                        return message;
                    }
                }
                else
                {
                    // read first message out of the incoming data
                    if (HandshakeProtocol.TryParseResponseMessage(ref buffer, out var responseMessage))
                    {
                        return responseMessage;
                    }
                }
            }
            finally
            {
                Connection.Application.Input.AdvanceTo(buffer.Start);
            }

            return null;
        }

        public void Dispose()
        {
            _cts.Cancel();

            Connection.Application.Output.Complete();
        }

        private static string GetInvocationId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    _heartbeatHandlers = new List<(Action<object> handler, object state)>();
                }
                _heartbeatHandlers.Add((action, state));
            }
        }

        public void TickHeartbeat()
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    return;
                }

                foreach (var (handler, state) in _heartbeatHandlers)
                {
                    handler(state);
                }
            }
        }

        public byte[] GetHandshakeRequestMessage()
        {
            var memoryBufferWriter = MemoryBufferWriter.Get();
            try
            {
                HandshakeProtocol.WriteRequestMessage(new HandshakeRequestMessage(_protocol.Name, _protocol.Version), memoryBufferWriter);
                return memoryBufferWriter.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(memoryBufferWriter);
            }
        }

        private class DefaultInvocationBinder : IInvocationBinder
        {
            public IReadOnlyList<Type> GetParameterTypes(string methodName)
            {
                // TODO: Possibly support actual client methods
                return new[] { typeof(object) };
            }

            public Type GetReturnType(string invocationId)
            {
                return typeof(object);
            }

            public Type GetStreamItemType(string streamId)
            {
                throw new NotImplementedException();
            }
        }
    }
}
