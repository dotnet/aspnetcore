// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Security;

namespace Microsoft.AspNet.Http
{
    public abstract class HttpResponse
    {
        // TODO - review IOwinResponse for completeness

        public abstract HttpContext HttpContext { get; }
        public abstract int StatusCode { get; set; }
        public abstract IHeaderDictionary Headers { get; }
        public abstract Stream Body { get; set; }

        public abstract long? ContentLength { get; set; }
        public abstract string ContentType { get; set; }

        public abstract IResponseCookies Cookies { get; }

        public abstract void OnSendingHeaders(Action<object> callback, object state);

        public virtual void Redirect(string location)
        {
            Redirect(location, permanent: false);
        }

        public abstract void Redirect(string location, bool permanent);

        public virtual void Challenge()
        {
            Challenge(new string[0]);
        }

        public virtual void Challenge(AuthenticationProperties properties)
        {
            Challenge(properties, new string[0]);
        }

        public virtual void Challenge(string authenticationType)
        {
            Challenge(new[] { authenticationType });
        }

        public virtual void Challenge(AuthenticationProperties properties, string authenticationType)
        {
            Challenge(properties, new[] { authenticationType });
        }

        public void Challenge(params string[] authenticationTypes)
        {
            Challenge((IEnumerable<string>)authenticationTypes);
        }

        public virtual void Challenge(IEnumerable<string> authenticationTypes)
        {
            Challenge(properties: null, authenticationTypes:  authenticationTypes);
        }

        public void Challenge(AuthenticationProperties properties, params string[] authenticationTypes)
        {
            Challenge(properties, (IEnumerable<string>)authenticationTypes);
        }

        public abstract void Challenge(AuthenticationProperties properties, IEnumerable<string> authenticationTypes);

        public virtual void SignIn(ClaimsIdentity identity)
        {
            SignIn(properties: null, identity: identity);
        }

        public virtual void SignIn(AuthenticationProperties properties, ClaimsIdentity identity)
        {
            SignIn(properties, new[] { identity });
        }

        public virtual void SignIn(params ClaimsIdentity[] identities)
        {
            SignIn(properties: null, identities: (IEnumerable<ClaimsIdentity>)identities);
        }

        public virtual void SignIn(IEnumerable<ClaimsIdentity> identities)
        {
            SignIn(properties: null, identities: identities);
        }

        public void SignIn(AuthenticationProperties properties, params ClaimsIdentity[] identities)
        {
            SignIn(properties, (IEnumerable<ClaimsIdentity>)identities);
        }

        public abstract void SignIn(AuthenticationProperties properties, IEnumerable<ClaimsIdentity> identities);

        public virtual void SignOut()
        {
            SignOut(new string[0]);
        }

        public virtual void SignOut(string authenticationType)
        {
            SignOut(new[] { authenticationType });
        }

        public abstract void SignOut(IEnumerable<string> authenticationTypes);
    }
}
