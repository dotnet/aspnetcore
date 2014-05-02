// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions.Security;

namespace Microsoft.AspNet.Abstractions
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
