// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointFeature
    {
        Endpoint Endpoint { get; set; }

        Func<RequestDelegate, RequestDelegate> Invoker { get; set; }

        RouteValueDictionary Values { get; set; }
    }
}
