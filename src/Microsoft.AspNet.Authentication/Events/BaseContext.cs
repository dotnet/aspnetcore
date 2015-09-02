// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication
{
    public abstract class BaseContext
    {
        protected BaseContext(HttpContext context)
        {
            HttpContext = context;
        }

        public HttpContext HttpContext { get; private set; }

        public HttpRequest Request
        {
            get { return HttpContext.Request; }
        }

        public HttpResponse Response
        {
            get { return HttpContext.Response; }
        }
    }
}
