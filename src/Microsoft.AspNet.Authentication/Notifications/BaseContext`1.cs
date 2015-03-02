// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.Notifications
{
    /// <summary>
    /// Base class used for certain event contexts
    /// </summary>
    public abstract class BaseContext<TOptions>
    {
        protected BaseContext(HttpContext context, TOptions options)
        {
            HttpContext = context;
            Options = options;
        }

        public HttpContext HttpContext { get; private set; }

        public TOptions Options { get; private set; }

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
