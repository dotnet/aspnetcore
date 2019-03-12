// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteJSRuntime : JSRuntimeBase
    {
        private IClientProxy _clientProxy;

        internal void Initialize(CircuitClientProxy clientProxy)
        {
            _clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            if (_clientProxy == CircuitClientProxy.OfflineClient)
            {
                var errorMessage = "JavaScript interop calls cannot be issued while the client is not connected, because the server is not able to interop with the browser at this time. " +
                    "Components must wrap any JavaScript interop calls in conditional logic to ensure those interop calls are not attempted during periods where the client is not connected.";
                throw new InvalidOperationException(errorMessage);
            }

            _clientProxy.SendAsync("JS.BeginInvokeJS", asyncHandle, identifier, argsJson);
        }
    }
}
