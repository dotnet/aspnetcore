// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Security;

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

        public abstract CancellationToken OnRequestAborted { get; }

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

        public abstract IEnumerable<AuthenticationDescription> GetAuthenticationTypes();

        public virtual AuthenticationResult Authenticate(string authenticationType)
        {
            return Authenticate(new[] { authenticationType }).SingleOrDefault();
        }

        public abstract IEnumerable<AuthenticationResult> Authenticate(IList<string> authenticationTypes);

        public virtual async Task<AuthenticationResult> AuthenticateAsync(string authenticationType)
        {
            return (await AuthenticateAsync(new[] { authenticationType })).SingleOrDefault();
        }

        public abstract Task<IEnumerable<AuthenticationResult>> AuthenticateAsync(IList<string> authenticationTypes);
    }
}
