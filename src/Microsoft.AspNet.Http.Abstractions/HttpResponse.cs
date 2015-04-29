// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.Http
{
    public abstract class HttpResponse
    {
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
    }
}
