// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Internal;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class TestClient : IDisposable, IInvocationBinder
    {
        private static int _id;
        private IHubProtocol _protocol;
        private CancellationTokenSource _cts;

        public ConnectionContext Connection { get; }
        public IChannelConnection<Message> Application { get; }
        public Task Connected => Connection.Metadata.Get<TaskCompletionSource<bool>>("ConnectedTask").Task;

        public TestClient()
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            Application = ChannelConnection.Create<Message>(input: applicationToTransport, output: transportToApplication);
            var transport = ChannelConnection.Create<Message>(input: transportToApplication, output: applicationToTransport);

            Connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), transport);
            Connection.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Interlocked.Increment(ref _id).ToString()) }));
            Connection.Metadata["ConnectedTask"] = new TaskCompletionSource<bool>();

            _protocol = new JsonHubProtocol(new JsonSerializer());

            _cts = new CancellationTokenSource();
        }

        public async Task<IList<HubMessage>> StreamAsync(string methodName, params object[] args)
        {
            var invocationId = await SendInvocationAsync(methodName, args);

            var messages = new List<HubMessage>();
            while (true)
            {
                var message = await Read();

                if (!string.Equals(message.InvocationId, invocationId))
                {
                    throw new NotSupportedException("TestClient does not support multiple outgoing invocations!");
                }

                if (message == null)
                {
                    throw new InvalidOperationException("Connection aborted!");
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
            var invocationId = await SendInvocationAsync(methodName, args);

            while (true)
            {
                var message = await Read();

                if (!string.Equals(message.InvocationId, invocationId))
                {
                    throw new NotSupportedException("TestClient does not support multiple outgoing invocations!");
                }

                if (message == null)
                {
                    throw new InvalidOperationException("Connection aborted!");
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

        public async Task<string> SendInvocationAsync(string methodName, params object[] args)
        {
            var invocationId = GetInvocationId();
            var payload = await _protocol.WriteToArrayAsync(new InvocationMessage(invocationId, nonBlocking: false, target: methodName, arguments: args));

            await Application.Output.WriteAsync(new Message(payload, _protocol.MessageType, endOfMessage: true));

            return invocationId;
        }

        public async Task<HubMessage> Read()
        {
            while (true)
            {
                var message = TryRead();

                if (message == null)
                {
                    if (!await Application.Input.WaitToReadAsync())
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
            if (Application.Input.TryRead(out var message))
            {
                return _protocol.ParseMessage(message.Payload, this);
            }
            return null;
        }

        public void Dispose()
        {
            _cts.Cancel();
            Connection.Dispose();
        }

        private static string GetInvocationId()
        {
            return Guid.NewGuid().ToString("N");
        }

        Type[] IInvocationBinder.GetParameterTypes(string methodName)
        {
            // TODO: Possibly support actual client methods
            return new[] { typeof(object) };
        }

        Type IInvocationBinder.GetReturnType(string invocationId)
        {
            return typeof(object);
        }
    }
}
