﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NET45

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Owin
{
    public class RouterMiddleware
    {
        public RouterMiddleware(Func<IDictionary<string, object>, Task> next, IRouteEngine engine)
        {
            Next = next;
            Engine = engine;
        }

        private IRouteEngine Engine
        {
            get;
            set;
        }

        private Func<IDictionary<string, object>, Task> Next
        {
            get;
            set;
        }

        public async Task Invoke(IDictionary<string, object> context)
        {
            if (!(await Engine.Invoke(context)))
            {
                await Next.Invoke(context);
            }
        }
    }
}

#endif