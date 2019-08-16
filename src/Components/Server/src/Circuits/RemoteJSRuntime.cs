// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteJSRuntime : JSRuntime
    {
        private readonly CircuitOptions _options;
        private readonly ILogger<RemoteJSRuntime> _logger;
        private CircuitClientProxy _clientProxy;

        public RemoteJSRuntime(IOptions<CircuitOptions> options, ILogger<RemoteJSRuntime> logger)
        {
            _options = options.Value;
            _logger = logger;
            DefaultAsyncTimeout = _options.JSInteropDefaultCallTimeout;
            JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter());
        }

        internal void Initialize(CircuitClientProxy clientProxy)
        {
            _clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        }

        protected override void EndInvokeDotNet(string callId, bool success, object resultOrError, string assemblyName, string methodIdentifier, long dotNetObjectId)
        {
            if (!success)
            {
                var actualException = resultOrError is Exception ex ? ex : resultOrError is ExceptionDispatchInfo edi ? edi.SourceException : resultOrError;
                Log.InvokeDotNetMethodException(_logger, callId, assemblyName, methodIdentifier, dotNetObjectId, actualException as Exception);
                if (_options.DetailedErrors)
                {
                    EndInvokeDotNetCore(callId, success, actualException.ToString());
                }
                else
                {
                    var message = $"There was an exception invoking '{methodIdentifier}' on assembly '{assemblyName}'. For more details turn on " +
                        $"detailed exceptions in '{typeof(CircuitOptions).Name}.{nameof(CircuitOptions.DetailedErrors)}'";

                    EndInvokeDotNetCore(callId, success, message);
                }
            }
            else
            {
                Log.InvokeDotNetMethodSuccess(_logger, callId, assemblyName, methodIdentifier, dotNetObjectId);
                EndInvokeDotNetCore(callId, success, resultOrError);
            }
        }

        private void EndInvokeDotNetCore(string callId, bool success, object resultOrError)
        {
            _clientProxy.SendAsync(
                "JS.EndInvokeDotNet",
                JsonSerializer.Serialize(new[] { callId, success, resultOrError }, JsonSerializerOptions));
        }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            if (!_clientProxy.Connected)
            {
                throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time. This is because the component is being " +
                    "prerendered and the page has not yet loaded in the browser or because the circuit is currently disconnected. " +
                    "Components must wrap any JavaScript interop calls in conditional logic to ensure those interop calls are not " +
                    "attempted during prerendering or while the client is disconnected.");
            }

            Log.BeginInvokeJS(_logger, asyncHandle, identifier);

            _clientProxy.SendAsync("JS.BeginInvokeJS", asyncHandle, identifier, argsJson);
        }

        public static class Log
        {
            private static readonly Action<ILogger, long, string, Exception> _beginInvokeJS =
                LoggerMessage.Define<long, string>(
                    LogLevel.Debug,
                    new EventId(1, "BeginInvokeJS"),
                    "Begin invoke JS interop '{AsyncHandle}': '{FunctionIdentifier}'");

            private static readonly Action<ILogger, string, string, string, Exception> _invokeStaticDotNetMethodException =
                LoggerMessage.Define<string, string, string>(
                LogLevel.Debug,
                new EventId(2, "InvokeDotNetMethodException"),
                "There was an error invoking the static method '[{AssemblyName}]::{MethodIdentifier}' with callback id '{CallbackId}'.");

            private static readonly Action<ILogger, string, long, string, Exception> _invokeInstanceDotNetMethodException =
                LoggerMessage.Define<string, long, string>(
                LogLevel.Debug,
                new EventId(2, "InvokeDotNetMethodException"),
                "There was an error invoking the instance method '{MethodIdentifier}' on reference '{DotNetObjectReference}' with callback id '{CallbackId}'.");

            private static readonly Action<ILogger, string, string, string, Exception> _invokeStaticDotNetMethodSuccess =
                LoggerMessage.Define<string, string, string>(
                LogLevel.Debug,
                new EventId(3, "InvokeDotNetMethodSuccess"),
                "Invocation of '[{AssemblyName}]::{MethodIdentifier}' with callback id '{CallbackId}' completed successfully.");

            private static readonly Action<ILogger, string, long, string, Exception> _invokeInstanceDotNetMethodSuccess =
                LoggerMessage.Define<string, long, string>(
                LogLevel.Debug,
                new EventId(3, "InvokeDotNetMethodSuccess"),
                "Invocation of '{MethodIdentifier}' on reference '{DotNetObjectReference}' with callback id '{CallbackId}' completed successfully.");


            internal static void BeginInvokeJS(ILogger logger, long asyncHandle, string identifier) =>
                _beginInvokeJS(logger, asyncHandle, identifier, null);

            internal static void InvokeDotNetMethodException(ILogger logger, string callId, string assemblyName, string methodIdentifier, long dotNetObjectReference, Exception exception)
            {
                if (assemblyName != null)
                {
                    _invokeStaticDotNetMethodException(logger, assemblyName, methodIdentifier, callId, exception);
                }
                else
                {
                    _invokeInstanceDotNetMethodException(logger, methodIdentifier, dotNetObjectReference, callId, exception);
                }
            }

            internal static void InvokeDotNetMethodSuccess(ILogger<RemoteJSRuntime> logger, string callId, string assemblyName, string methodIdentifier, long dotNetObjectId)
            {
                if (assemblyName != null)
                {
                    _invokeStaticDotNetMethodSuccess(logger, assemblyName, methodIdentifier, callId, null);
                }
                else
                {
                    _invokeInstanceDotNetMethodSuccess(logger, methodIdentifier, dotNetObjectId, callId, null);
                }

            }
        }
    }
}
