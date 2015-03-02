// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Http
{
    public abstract class HttpContext : IDisposable
    {
        public abstract HttpRequest Request { get; }

        public abstract HttpResponse Response { get; }

        public abstract ClaimsPrincipal User { get; set; }
        
        public abstract IDictionary<object, object> Items { get; }

        public abstract IServiceProvider ApplicationServices { get; set; }

        public abstract IServiceProvider RequestServices { get; set; }

        public abstract CancellationToken RequestAborted { get; }

        public abstract ISessionCollection Session { get; }

        public abstract bool IsWebSocketRequest { get; }

        public abstract IList<string> WebSocketRequestedProtocols { get; }

        public abstract void Abort();

        public abstract void Dispose();

        public abstract object GetFeature(Type type);

        public abstract void SetFeature(Type type, object instance);

        public virtual T GetFeature<T>()
        {
            return (T)GetFeature(typeof(T));
        }

        public virtual void SetFeature<T>(T instance)
        {
            SetFeature(typeof(T), instance);
        }

        public abstract IEnumerable<AuthenticationDescription> GetAuthenticationSchemes();

        public virtual AuthenticationResult Authenticate(string authenticationScheme)
        {
            return Authenticate(new[] { authenticationScheme }).SingleOrDefault();
        }

        public abstract IEnumerable<AuthenticationResult> Authenticate(IEnumerable<string> authenticationSchemes);

        public virtual async Task<AuthenticationResult> AuthenticateAsync(string authenticationScheme)
        {
            return (await AuthenticateAsync(new[] { authenticationScheme })).SingleOrDefault();
        }

        public abstract Task<IEnumerable<AuthenticationResult>> AuthenticateAsync(IEnumerable<string> authenticationSchemes);

        public virtual Task<WebSocket> AcceptWebSocketAsync()
        {
            return AcceptWebSocketAsync(subProtocol: null);
        }

        public abstract Task<WebSocket> AcceptWebSocketAsync(string subProtocol);
    }
}
