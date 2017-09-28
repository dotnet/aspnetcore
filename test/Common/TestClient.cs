// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Internal;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class TestClient : IDisposable
    {
        private static int _id;
        private readonly HubProtocolReaderWriter _protocolReaderWriter;
        private readonly IInvocationBinder _invocationBinder;
        private CancellationTokenSource _cts;
        private ChannelConnection<byte[]> _transport;


        public DefaultConnectionContext Connection { get; }
        public Channel<byte[]> Application { get; }
        public Task Connected => ((TaskCompletionSource<bool>)Connection.Metadata["ConnectedTask"]).Task;

        public TestClient(bool synchronousCallbacks = false, IHubProtocol protocol = null, IInvocationBinder invocationBinder = null, bool addClaimId = false)
        {
            var options = new ChannelOptimizations { AllowSynchronousContinuations = synchronousCallbacks };
            var transportToApplication = Channel.CreateUnbounded<byte[]>(options);
            var applicationToTransport = Channel.CreateUnbounded<byte[]>(options);

            Application = ChannelConnection.Create<byte[]>(input: applicationToTransport, output: transportToApplication);
            _transport = ChannelConnection.Create<byte[]>(input: transportToApplication, output: applicationToTransport);

            Connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), _transport, Application);

            var claimValue = Interlocked.Increment(ref _id).ToString();
            var claims = new List<Claim>{ new Claim(ClaimTypes.Name, claimValue) };
            if (addClaimId)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claimValue));
            }

            Connection.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            Connection.Metadata["ConnectedTask"] = new TaskCompletionSource<bool>();

            protocol = protocol ?? new JsonHubProtocol();
            _protocolReaderWriter = new HubProtocolReaderWriter(protocol, new PassThroughEncoder());
            _invocationBinder = invocationBinder ?? new DefaultInvocationBinder();

            _cts = new CancellationTokenSource();

            using (var memoryStream = new MemoryStream())
            {
                NegotiationProtocol.WriteMessage(new NegotiationMessage(protocol.Name), memoryStream);
                Application.Out.TryWrite(memoryStream.ToArray());
            }
        }

        public async Task<IList<HubMessage>> StreamAsync(string methodName, params object[] args)
        {
            var invocationId = await SendInvocationAsync(methodName, nonBlocking: false, args: args);

            var messages = new List<HubMessage>();
            while (true)
            {
                var message = await ReadAsync();

                if (message == null)
                {
                    throw new InvalidOperationException("Connection aborted!");
                }

                if (!string.Equals(message.InvocationId, invocationId))
                {
                    throw new NotSupportedException("TestClient does not support multiple outgoing invocations!");
                }

                switch (message)
                {
                    case StreamItemMessage _:
                        messages.Add(message);
                        break;
                    case CompletionMessage _:
                    case StreamCompletionMessage _:
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

                if (!string.Equals(message.InvocationId, invocationId))
                {
                    throw new NotSupportedException("TestClient does not support multiple outgoing invocations!");
                }

                switch (message)
                {
                    case StreamItemMessage result:
                        throw new NotSupportedException("Use 'StreamAsync' to call a streaming method");
                    case CompletionMessage completion:
                        return completion;
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
            var invocationId = GetInvocationId();
            return SendHubMessageAsync(new InvocationMessage(invocationId, nonBlocking, methodName, args));
        }

        public async Task<string> SendHubMessageAsync(HubMessage message)
        {
            var payload = _protocolReaderWriter.WriteMessage(message);
            await Application.Out.WriteAsync(payload);
            return message.InvocationId;
        }

        public async Task<HubMessage> ReadAsync()
        {
            while (true)
            {
                var message = TryRead();

                if (message == null)
                {
                    if (!await Application.In.WaitToReadAsync())
                    {
                        return null;
                    }
                }
                else
                {
                    return message;
                }
            }
        }

        public HubMessage TryRead()
        {
            if (Application.In.TryRead(out var buffer) &&
                _protocolReaderWriter.ReadMessages(buffer, _invocationBinder, out var messages))
            {
                return messages[0];
            }
            return null;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _transport.Dispose();
        }

        private static string GetInvocationId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private class DefaultInvocationBinder : IInvocationBinder
        {
            public Type[] GetParameterTypes(string methodName)
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