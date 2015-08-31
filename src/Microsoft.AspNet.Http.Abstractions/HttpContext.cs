// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Http
{
    public abstract class HttpContext : IDisposable
    {
        public abstract IFeatureCollection Features { get; }

        public abstract HttpRequest Request { get; }

        public abstract HttpResponse Response { get; }

        public abstract ConnectionInfo Connection { get; }

        public abstract WebSocketManager WebSockets { get; }

        public abstract AuthenticationManager Authentication { get; }

        public abstract ClaimsPrincipal User { get; set; }

        public abstract IDictionary<object, object> Items { get; set; }

        public abstract IServiceProvider ApplicationServices { get; set; }

        public abstract IServiceProvider RequestServices { get; set; }

        public abstract CancellationToken RequestAborted { get; set; }

        public abstract ISession Session { get; set; }

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
    }
}
