// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A feature in <see cref="Microsoft.AspNetCore.Http.HttpContext.Features"/> which is used to capture the
    /// currently executing context of a resource filter. This feature is used in the final middleware
    /// of a middleware filter's pipeline to keep the request flow through the rest of the MVC layers.
    /// </summary>
    internal interface IMiddlewareFilterFeature
    {
        ResourceExecutingContext ResourceExecutingContext { get; }

        ResourceExecutionDelegate ResourceExecutionDelegate { get; }
    }
}
