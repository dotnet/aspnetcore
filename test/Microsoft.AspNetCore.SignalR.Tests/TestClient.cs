// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class TestClient : IDisposable
    {
        private static int _id;
        private IInvocationAdapter _adapter;
        private CancellationTokenSource _cts;
        private TestBinder _binder;

        public Connection Connection;
        public IChannelConnection<Message> Application { get; }
        public Task Connected => Connection.Metadata.Get<TaskCompletionSource<bool>>("ConnectedTask").Task;

        public TestClient(IServiceProvider serviceProvider, string format = "json")
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            Application = ChannelConnection.Create(input: applicationToTransport, output: transportToApplication);
            var transport = ChannelConnection.Create(input: transportToApplication, output: applicationToTransport);

            Connection = new Connection(Guid.NewGuid().ToString(), transport);
            Connection.Metadata["formatType"] = format;
            Connection.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, Interlocked.Increment(ref _id).ToString()) }));
            Connection.Metadata["ConnectedTask"] = new TaskCompletionSource<bool>();

            var invocationAdapter = serviceProvider.GetService<InvocationAdapterRegistry>();
            _adapter = invocationAdapter.GetInvocationAdapter(format);

            _binder = new TestBinder();

            _cts = new CancellationTokenSource();
        }

        public async Task<T> Invoke<T>(string methodName, params object[] args) where T : InvocationMessage
        {
            await Invoke(methodName, args);

            return await Read<T>();
        }

        public async Task Invoke(string methodName, params object[] args)
        {
            var stream = new MemoryStream();
            await _adapter.WriteMessageAsync(new InvocationDescriptor
            {
                Arguments = args,
                Method = methodName
            },
            stream);

            await Application.Output.WriteAsync(new Message(stream.ToArray(), MessageType.Binary, endOfMessage: true));
        }

        public async Task<T> Read<T>() where T : InvocationMessage
        {
            while (await Application.Input.WaitToReadAsync(_cts.Token))
            {
                var value = await TryRead<T>();

                if (value != null)
                {
                    return value;
                }
            }

            return null;
        }

        public async Task<T> TryRead<T>() where T : InvocationMessage
        {
            Message message;
            if (Application.Input.TryRead(out message))
            {
                var value = await _adapter.ReadMessageAsync(new MemoryStream(message.Payload), _binder);
                return value as T;
            }

            return null;
        }

        public void Dispose()
        {
            _cts.Cancel();
            Connection.Dispose();
        }

        private class TestBinder : IInvocationBinder
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
