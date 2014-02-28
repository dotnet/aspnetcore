﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing
{
    public interface IRouteEngine
    {
        IRouteCollection Routes { get; }

        Task<bool> Invoke(HttpContext context);

        string GetUrl(HttpContext context, IDictionary<string, object> values);
    }
}
