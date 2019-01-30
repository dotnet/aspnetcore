// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteJSRuntime : JSRuntimeBase
    {
        private readonly IClientProxy _clientProxy;

        public RemoteJSRuntime(IClientProxy clientProxy)
        {
            _clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            _clientProxy.SendAsync("JS.BeginInvokeJS", asyncHandle, identifier, argsJson);
        }
    }
}
