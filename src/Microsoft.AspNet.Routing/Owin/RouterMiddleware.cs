﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing.Owin
{
    public class RouterMiddleware
    {
        public RouterMiddleware(RequestDelegate next, IRouteEngine engine)
        {
            Next = next;
            Engine = engine;
        }

        private IRouteEngine Engine
        {
            get;
            set;
        }

        private RequestDelegate Next
        {
            get;
            set;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!(await Engine.Invoke(context)))
            {
                await Next.Invoke(context);
            }
        }
    }
}
