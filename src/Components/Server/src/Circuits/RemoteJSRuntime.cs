// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteJSRuntime : JSRuntime
    {
        private readonly CircuitOptions _options;
        private readonly ILogger<RemoteJSRuntime> _logger;
        private CircuitClientProxy _clientProxy;
        private bool _permanentlyDisconnected;
        private readonly long _maximumIncomingBytes;
        private int _byteArraysToBeRevivedTotalBytes;

        public ElementReferenceContext ElementReferenceContext { get; }

        public bool IsInitialized => _clientProxy is not null;

        public RemoteJSRuntime(
            IOptions<CircuitOptions> circuitOptions,
            IOptions<HubOptions> hubOptions,
            ILogger<RemoteJSRuntime> logger)
        {
            _options = circuitOptions.Value;
            _maximumIncomingBytes = hubOptions.Value.MaximumReceiveMessageSize is null
                ? long.MaxValue
                : hubOptions.Value.MaximumReceiveMessageSize.Value;
            _logger = logger;
            DefaultAsyncTimeout = _options.JSInteropDefaultCallTimeout;
            ElementReferenceContext = new WebElementReferenceContext(this);
            JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter(ElementReferenceContext));
        }

        public JsonSerializerOptions ReadJsonSerializerOptions() => JsonSerializerOptions;

        internal void Initialize(CircuitClientProxy clientProxy)
        {
            _clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        }

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            if (!invocationResult.Success)
            {
                Log.InvokeDotNetMethodException(_logger, invocationInfo, invocationResult.Exception);
                string errorMessage;

                if (_options.DetailedErrors)
                {
                    errorMessage = invocationResult.Exception.ToString();
                }
                else
                {
                    errorMessage = $"There was an exception invoking '{invocationInfo.MethodIdentifier}'";
                    if (invocationInfo.AssemblyName != null)
                    {
                        errorMessage += $" on assembly '{invocationInfo.AssemblyName}'";
                    }

                    errorMessage += $". For more details turn on detailed exceptions in '{nameof(CircuitOptions)}.{nameof(CircuitOptions.DetailedErrors)}'";
                }

                _clientProxy.SendAsync("JS.EndInvokeDotNet",
                    invocationInfo.CallId,
                    /* success */ false,
                    errorMessage);
            }
            else
            {
                Log.InvokeDotNetMethodSuccess(_logger, invocationInfo);
                _clientProxy.SendAsync("JS.EndInvokeDotNet",
                    invocationInfo.CallId,
                    /* success */ true,
                    invocationResult.ResultJson);
            }
        }

        protected override void SendByteArray(int id, byte[] data)
        {
            _clientProxy.SendAsync("JS.ReceiveByteArray", id, data);
        }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            if (_clientProxy is null)
            {
                if (_permanentlyDisconnected)
                {
                    throw new JSDisconnectedException(
                   "JavaScript interop calls cannot be issued at this time. This is because the circuit has disconnected " +
                   "and is being disposed.");
                }
                else
                {
                    throw new InvalidOperationException(
                        "JavaScript interop calls cannot be issued at this time. This is because the component is being " +
                        "statically rendered. When prerendering is enabled, JavaScript interop calls can only be performed " +
                        "during the OnAfterRenderAsync lifecycle method.");
                }
            }

            Log.BeginInvokeJS(_logger, asyncHandle, identifier);

            _clientProxy.SendAsync("JS.BeginInvokeJS", asyncHandle, identifier, argsJson, (int)resultType, targetInstanceId);
        }

        protected override void ReceiveByteArray(int id, byte[] data)
        {
            if (id == 0)
            {
                // Starting a new transfer, clear out number of bytes read.
                _byteArraysToBeRevivedTotalBytes = 0;
            }

            if (_maximumIncomingBytes - data.Length < _byteArraysToBeRevivedTotalBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "Exceeded the maximum byte array transfer limit for a call.");
            }

            // We also store the total number of bytes seen so far to compare against
            // the MaximumIncomingBytes limit.
            // We take the larger of the size of the array or 4, to ensure we're not inundated
            // with small/empty arrays.
            _byteArraysToBeRevivedTotalBytes += Math.Max(4, data.Length);

            base.ReceiveByteArray(id, data);
        }

        public void MarkPermanentlyDisconnected()
        {
            _permanentlyDisconnected = true;
            _clientProxy = null;
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

            internal static void InvokeDotNetMethodException(ILogger logger, in DotNetInvocationInfo invocationInfo, Exception exception)
            {
                if (invocationInfo.AssemblyName != null)
                {
                    _invokeStaticDotNetMethodException(logger, invocationInfo.AssemblyName, invocationInfo.MethodIdentifier, invocationInfo.CallId, exception);
                }
                else
                {
                    _invokeInstanceDotNetMethodException(logger, invocationInfo.MethodIdentifier, invocationInfo.DotNetObjectId, invocationInfo.CallId, exception);
                }
            }

            internal static void InvokeDotNetMethodSuccess(ILogger<RemoteJSRuntime> logger, in DotNetInvocationInfo invocationInfo)
            {
                if (invocationInfo.AssemblyName != null)
                {
                    _invokeStaticDotNetMethodSuccess(logger, invocationInfo.AssemblyName, invocationInfo.MethodIdentifier, invocationInfo.CallId, null);
                }
                else
                {
                    _invokeInstanceDotNetMethodSuccess(logger, invocationInfo.MethodIdentifier, invocationInfo.DotNetObjectId, invocationInfo.CallId, null);
                }

            }
        }
    }
}
