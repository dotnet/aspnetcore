// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class TestClient : IDisposable
    {
        private static int _id;
        private readonly IHubProtocol _protocol;
        private readonly IInvocationBinder _invocationBinder;
        private readonly CancellationTokenSource _cts;
        private readonly Queue<HubMessage> _messages = new Queue<HubMessage>();

        public DefaultConnectionContext Connection { get; }
        public Task Connected => ((TaskCompletionSource<bool>)Connection.Items["ConnectedTask"]).Task;
        public HandshakeResponseMessage HandshakeResponseMessage { get; private set; }

        public TestClient(bool synchronousCallbacks = false, IHubProtocol protocol = null, IInvocationBinder invocationBinder = null, bool addClaimId = false)
        {
            var options = new PipeOptions(readerScheduler: synchronousCallbacks ? PipeScheduler.Inline : null);
            var pair = DuplexPipe.CreateConnectionPair(options, options);
            Connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Transport, pair.Application);

            var claimValue = Interlocked.Increment(ref _id).ToString();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, claimValue) };
            if (addClaimId)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claimValue));
            }

            Connection.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            Connection.Items["ConnectedTask"] = new TaskCompletionSource<bool>();

            _protocol = protocol ?? new JsonHubProtocol();
            _invocationBinder = invocationBinder ?? new DefaultInvocationBinder();

            _cts = new CancellationTokenSource();
        }

        public async Task<Task> ConnectAsync(
            EndPoint endPoint,
            bool sendHandshakeRequestMessage = true,
            bool expectedHandshakeResponseMessage = true)
        {
            if (sendHandshakeRequestMessage)
            {
                using (var memoryStream = new MemoryStream())
                {
                    HandshakeProtocol.WriteRequestMessage(new HandshakeRequestMessage(_protocol.Name, _protocol.Version), memoryStream);
                    await Connection.Application.Output.WriteAsync(memoryStream.ToArray());
                }
            }

            var connection = endPoint.OnConnectedAsync(Connection);

            if (expectedHandshakeResponseMessage)
            {
                // note that the handshake response might not immediately be readable
                // e.g. server is waiting for request, times out after configured duration,
                // and sends response with timeout error
                HandshakeResponseMessage = (HandshakeResponseMessage) await ReadAsync(true).OrTimeout();
            }

            return connection;
        }

        public async Task<IList<HubMessage>> StreamAsync(string methodName, params object[] args)
        {
            var invocationId = await SendStreamInvocationAsync(methodName, args);

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
                        throw new NotSupportedException("TestClient does not support receiving invocations!");
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
                        throw new NotSupportedException("TestClient does not support receiving invocations!");
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
            return SendHubMessageAsync(new InvocationMessage(invocationId, methodName,
                argumentBindingException: null, arguments: args));
        }

        public Task<string> SendStreamInvocationAsync(string methodName, params object[] args)
        {
            var invocationId = GetInvocationId();
            return SendHubMessageAsync(new StreamInvocationMessage(invocationId, methodName,
                argumentBindingException: null, arguments: args));
        }

        public async Task<string> SendHubMessageAsync(HubMessage message)
        {
            var payload = _protocol.WriteToArray(message);

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
            if (_messages.Count > 0)
            {
                return _messages.Dequeue();
            }

            if (!Connection.Application.Input.TryRead(out var result))
            {
                return null;
            }

            var buffer = result.Buffer;
            var consumed = buffer.End;
            var examined = consumed;

            try
            {
                if (!isHandshake)
                {
                    var messages = new List<HubMessage>();
                    if (_protocol.TryParseMessages(result.Buffer.ToArray(), _invocationBinder, messages))
                    {
                        foreach (var m in messages)
                        {
                            _messages.Enqueue(m);
                        }

                        return _messages.Dequeue();
                    }
                }
                else
                {
                    HandshakeProtocol.TryReadMessageIntoSingleMemory(buffer, out consumed, out examined, out var data);

                    // read first message out of the incoming data
                    if (!TextMessageParser.TryParseMessage(ref data, out var payload))
                    {
                        throw new InvalidDataException("Unable to parse payload as a handshake response message.");
                    }

                    return HandshakeProtocol.ParseResponseMessage(payload);
                }
            }
            finally
            {
                Connection.Application.Input.AdvanceTo(consumed, examined);
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
        }
    }
}
