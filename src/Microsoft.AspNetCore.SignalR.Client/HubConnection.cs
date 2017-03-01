// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class HubConnection
    {
        private readonly ILogger _logger;
        private readonly IConnection _connection;
        private readonly IInvocationAdapter _adapter;
        private readonly HubBinder _binder;

        private readonly CancellationTokenSource _connectionActive = new CancellationTokenSource();

        // We need to ensure pending calls added after a connection failure don't hang. Right now the easiest thing to do is lock.
        private readonly object _pendingCallsLock = new object();
        private readonly Dictionary<string, InvocationRequest> _pendingCalls = new Dictionary<string, InvocationRequest>();

        private readonly ConcurrentDictionary<string, InvocationHandler> _handlers = new ConcurrentDictionary<string, InvocationHandler>();

        private int _nextId = 0;

        public event Action Connected
        {
            add { _connection.Connected += value; }
            remove { _connection.Connected -= value; }
        }

        public event Action<Exception> Closed
        {
            add { _connection.Closed += value; }
            remove { _connection.Closed -= value; }
        }

        public HubConnection(Uri url)
            : this(new Connection(url), new JsonNetInvocationAdapter(), null)
        { }

        public HubConnection(Uri url, ILoggerFactory loggerFactory)
            : this(new Connection(url), new JsonNetInvocationAdapter(), loggerFactory)
        { }

        public HubConnection(Uri url, IInvocationAdapter adapter, ILoggerFactory loggerFactory)
            : this(new Connection(url, loggerFactory), adapter, loggerFactory)
        { }

        public HubConnection(IConnection connection, IInvocationAdapter adapter, ILoggerFactory loggerFactory)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            _connection = connection;
            _binder = new HubBinder(this);
            _adapter = adapter;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<HubConnection>();
            _connection.Received += OnDataReceived;
            _connection.Closed += Shutdown;
        }

        public Task StartAsync() => StartAsync(null, null);
        public Task StartAsync(HttpClient httpClient) => StartAsync(null, httpClient);
        public Task StartAsync(ITransport transport) => StartAsync(transport, null);

        public async Task StartAsync(ITransport transport, HttpClient httpClient)
        {
            await _connection.StartAsync(transport, httpClient);
        }

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        // TODO: Client return values/tasks?
        // TODO: Overloads for void hub methods
        // TODO: Overloads that use type parameters (like On<T1>, On<T1, T2>, etc.)
        public void On(string methodName, Type[] parameterTypes, Action<object[]> handler)
        {
            var invocationHandler = new InvocationHandler(parameterTypes, handler);
            _handlers.AddOrUpdate(methodName, invocationHandler, (_, __) => invocationHandler);
        }

        public Task<T> Invoke<T>(string methodName, params object[] args) => Invoke<T>(methodName, CancellationToken.None, args);
        public async Task<T> Invoke<T>(string methodName, CancellationToken cancellationToken, params object[] args) => ((T)(await Invoke(methodName, typeof(T), cancellationToken, args)));

        public Task<object> Invoke(string methodName, Type returnType, params object[] args) => Invoke(methodName, returnType, CancellationToken.None, args);
        public async Task<object> Invoke(string methodName, Type returnType, CancellationToken cancellationToken, params object[] args)
        {
            // TODO: we should reject calls to here after the connection is "done" or has not been started (e.g. sending an invocation failed)

            _logger.LogTrace("Preparing invocation of '{0}', with return type '{1}' and {2} args", methodName, returnType.AssemblyQualifiedName, args.Length);

            // Create an invocation descriptor.
            var descriptor = new InvocationDescriptor
            {
                Id = GetNextId(),
                Method = methodName,
                Arguments = args
            };

            // I just want an excuse to use 'irq' as a variable name...
            _logger.LogDebug("Registering Invocation ID '{0}' for tracking", descriptor.Id);
            var irq = new InvocationRequest(cancellationToken, returnType);

            lock (_pendingCallsLock)
            {
                if (_connectionActive.IsCancellationRequested)
                {
                    throw new InvalidOperationException("Connection has been terminated.");
                }
                _pendingCalls.Add(descriptor.Id, irq);
            }

            // Trace the invocation, but only if that logging level is enabled (because building the args list is a bit slow)
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var argsList = string.Join(", ", args.Select(a => a.GetType().FullName));
                _logger.LogTrace("Invocation #{0}: {1} {2}({3})", descriptor.Id, returnType.FullName, methodName, argsList);
            }

            var ms = new MemoryStream();
            await _adapter.WriteMessageAsync(descriptor, ms, cancellationToken);

            _logger.LogInformation("Sending Invocation #{0}", descriptor.Id);

            // TODO: Format.Text - who, where and when decides about the format of outgoing messages
            // TODO HIGH: Handle return value/Exception from SendAsync
            await _connection.SendAsync(ms.ToArray(), MessageType.Text, cancellationToken);
            _logger.LogInformation("Sending Invocation #{0} complete", descriptor.Id);

            // Return the completion task. It will be completed by ReceiveMessages when the response is received.
            return await irq.Completion.Task;
        }

        private async void OnDataReceived(byte[] data, MessageType messageType)
        {
            var message
                = await _adapter.ReadMessageAsync(new MemoryStream(data), _binder, _connectionActive.Token);

            switch (message)
            {
                case InvocationDescriptor invocationDescriptor:
                    DispatchInvocation(invocationDescriptor, _connectionActive.Token);
                    break;
                case InvocationResultDescriptor invocationResultDescriptor:
                    InvocationRequest irq;
                    lock (_pendingCallsLock)
                    {
                        _connectionActive.Token.ThrowIfCancellationRequested();
                        irq = _pendingCalls[invocationResultDescriptor.Id];
                        _pendingCalls.Remove(invocationResultDescriptor.Id);
                    }
                    DispatchInvocationResult(invocationResultDescriptor, irq, _connectionActive.Token);
                    break;
            }
        }

        private void Shutdown(Exception ex = null)
        {
            _logger.LogTrace("Shutting down connection");
            if (ex != null)
            {
                _logger.LogError("Connection is shutting down due to an error: {0}", ex);
            }

            lock (_pendingCallsLock)
            {
                _connectionActive.Cancel();
                foreach (var call in _pendingCalls.Values)
                {
                    if (ex != null)
                    {
                        call.Completion.TrySetException(ex);
                    }
                    else
                    {
                        call.Completion.TrySetCanceled();
                    }
                }
                _pendingCalls.Clear();
            }
        }

        private void DispatchInvocation(InvocationDescriptor invocationDescriptor, CancellationToken cancellationToken)
        {
            // Find the handler
            if (!_handlers.TryGetValue(invocationDescriptor.Method, out InvocationHandler handler))
            {
                _logger.LogWarning("Failed to find handler for '{0}' method", invocationDescriptor.Method);
            }

            // TODO: Return values
            // TODO: Dispatch to a sync context to ensure we aren't blocking this loop.
            handler.Handler(invocationDescriptor.Arguments);
        }

        private void DispatchInvocationResult(InvocationResultDescriptor result, InvocationRequest irq, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received Result for Invocation #{0}", result.Id);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Debug.Assert(irq.Completion != null, "Didn't properly capture InvocationRequest in callback for ReadInvocationResultDescriptorAsync");

            // If the invocation hasn't been cancelled, dispatch the result
            if (!irq.CancellationToken.IsCancellationRequested)
            {
                irq.Registration.Dispose();

                // Complete the request based on the result
                // TODO: the TrySetXYZ methods will cause continuations attached to the Task to run, so we should dispatch to a sync context or thread pool.
                if (!string.IsNullOrEmpty(result.Error))
                {
                    _logger.LogInformation("Completing Invocation #{0} with error: {1}", result.Id, result.Error);
                    irq.Completion.TrySetException(new Exception(result.Error));
                }
                else
                {
                    _logger.LogInformation("Completing Invocation #{0} with result of type: {1}", result.Id, result.Result?.GetType()?.FullName ?? "<<void>>");
                    irq.Completion.TrySetResult(result.Result);
                }
            }
        }

        private string GetNextId() => Interlocked.Increment(ref _nextId).ToString();

        private class HubBinder : IInvocationBinder
        {
            private HubConnection _connection;

            public HubBinder(HubConnection connection)
            {
                _connection = connection;
            }

            public Type GetReturnType(string invocationId)
            {
                if (!_connection._pendingCalls.TryGetValue(invocationId, out InvocationRequest irq))
                {
                    _connection._logger.LogError("Unsolicited response received for invocation '{0}'", invocationId);
                    return null;
                }
                return irq.ResultType;
            }

            public Type[] GetParameterTypes(string methodName)
            {
                if (!_connection._handlers.TryGetValue(methodName, out InvocationHandler handler))
                {
                    _connection._logger.LogWarning("Failed to find handler for '{0}' method", methodName);
                    return Type.EmptyTypes;
                }
                return handler.ParameterTypes;
            }
        }

        private struct InvocationHandler
        {
            public Action<object[]> Handler { get; }
            public Type[] ParameterTypes { get; }

            public InvocationHandler(Type[] parameterTypes, Action<object[]> handler)
            {
                Handler = handler;
                ParameterTypes = parameterTypes;
            }
        }

        private struct InvocationRequest
        {
            public Type ResultType { get; }
            public CancellationToken CancellationToken { get; }
            public CancellationTokenRegistration Registration { get; }
            public TaskCompletionSource<object> Completion { get; }

            public InvocationRequest(CancellationToken cancellationToken, Type resultType)
            {
                var tcs = new TaskCompletionSource<object>();
                Completion = tcs;
                CancellationToken = cancellationToken;
                Registration = cancellationToken.Register(() => tcs.TrySetCanceled());
                ResultType = resultType;
            }
        }
    }
}
