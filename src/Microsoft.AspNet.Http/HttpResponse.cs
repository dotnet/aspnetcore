// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNet.Http.Authentication;

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

        public abstract bool HeadersSent { get; }

        public abstract void OnSendingHeaders(Action<object> callback, object state);

        public abstract void OnResponseCompleted(Action<object> callback, object state);

        public virtual void Redirect(string location)
        {
            Redirect(location, permanent: false);
        }

        public abstract void Redirect(string location, bool permanent);

        public virtual void Challenge()
        {
            Challenge(properties: null, authenticationScheme: null);
        }

        public virtual void Challenge(AuthenticationProperties properties)
        {
            Challenge(properties, "");
        }

        public virtual void Challenge(string authenticationScheme)
        {
            Challenge(properties: null, authenticationScheme: authenticationScheme);
        }

        public abstract void Challenge(AuthenticationProperties properties, string authenticationScheme);

        public abstract void SignIn(string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties = null);

        public virtual void SignOut()
        {
            SignOut(authenticationScheme: null, properties: null);
        }

        public abstract void SignOut(string authenticationScheme);

        public abstract void SignOut(string authenticationScheme, AuthenticationProperties properties);
    }
}
