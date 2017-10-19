// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public abstract class Template
    {
        public virtual string GetUrl()
        {
            return GetUrl(null, new DispatcherValueCollection());
        }

        public virtual string GetUrl(HttpContext httpContext)
        {
            return GetUrl(httpContext, new DispatcherValueCollection());
        }

        public virtual string GetUrl(DispatcherValueCollection values)
        {
            return GetUrl(null, values);
        }
        
        public abstract string GetUrl(HttpContext httpContext, DispatcherValueCollection values);
    }
}
