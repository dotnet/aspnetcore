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

        public abstract Task WriteAsync(string data);

        public virtual void Challenge()
        {
            Challenge(new string[0]);
        }

        public virtual void Challenge(AuthenticationProperties properties)
        {
            Challenge(new string[0], properties);
        }

        public virtual void Challenge(string authenticationType)
        {
            Challenge(new[] { authenticationType });
        }

        public virtual void Challenge(string authenticationType, AuthenticationProperties properties)
        {
            Challenge(new[] { authenticationType }, properties);
        }

        public virtual void Challenge(IList<string> authenticationTypes)
        {
            Challenge(authenticationTypes, properties: null);
        }

        public abstract void Challenge(IList<string> authenticationTypes, AuthenticationProperties properties);

        public virtual void SignIn(ClaimsIdentity identity)
        {
            SignIn(identity, properties: null);
        }

        public virtual void SignIn(ClaimsIdentity identity, AuthenticationProperties properties)
        {
            SignIn(new[] { identity }, properties);
        }

        public virtual void SignIn(IList<ClaimsIdentity> identities)
        {
            SignIn(identities, properties: null);
        }

        public abstract void SignIn(IList<ClaimsIdentity> identities, AuthenticationProperties properties);

        public virtual void SignOut()
        {
            SignOut(new string[0]);
        }

        public virtual void SignOut(string authenticationType)
        {
            SignOut(new[] { authenticationType });
        }

        public abstract void SignOut(IList<string> authenticationTypes);
    }
}
