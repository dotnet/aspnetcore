// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteJSRuntime : JSRuntimeBase
    {
        private readonly CircuitOptions _options;
        private CircuitClientProxy _clientProxy;

        public RemoteJSRuntime(IOptions<CircuitOptions> options)
        {
            _options = options.Value;
            DefaultAsyncTimeout = _options.JSInteropDefaultCallTimeout;
        }

        internal void Initialize(CircuitClientProxy clientProxy)
        {
            _clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        }

        protected override object OnDotNetInvocationException(Exception exception, string assemblyName, string methodIdentifier)
        {
            if (_options.JSInteropDetailedErrors)
            {
                return base.OnDotNetInvocationException(exception, assemblyName, methodIdentifier);
            }

            var message = $"There was an exception invoking '{methodIdentifier}' on assembly '{assemblyName}'. For more details turn on " +
                $"detailed exceptions in '{typeof(CircuitOptions).Name}.{nameof(CircuitOptions.JSInteropDetailedErrors)}'";

            return message;
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

            _clientProxy.SendAsync("JS.BeginInvokeJS", asyncHandle, identifier, argsJson);
        }
    }
}
