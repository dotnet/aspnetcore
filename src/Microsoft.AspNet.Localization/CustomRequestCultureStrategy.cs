// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Localization
{
    public class CustomRequestCultureStrategy : IRequestCultureStrategy
    {
        private readonly Func<HttpContext, RequestCulture> _strategy;

        public CustomRequestCultureStrategy([NotNull] Func<HttpContext, RequestCulture> strategy)
        {
            _strategy = strategy;
        }

        public RequestCulture DetermineRequestCulture([NotNull] HttpContext httpContext)
        {
            return _strategy(httpContext);
        }
    }
}
