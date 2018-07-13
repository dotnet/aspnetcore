// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public abstract class MatchProcessor
    {
        public virtual void Initialize(string parameterName, string constraintArgument)
        {
        }

        public abstract bool ProcessInbound(HttpContext httpContext, RouteValueDictionary values);

        public abstract bool ProcessOutbound(HttpContext httpContext, RouteValueDictionary values);
    }
}
