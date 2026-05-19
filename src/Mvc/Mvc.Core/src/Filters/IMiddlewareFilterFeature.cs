// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A feature in <see cref="Microsoft.AspNetCore.Http.HttpContext.Features"/> which is used to capture the
/// currently executing context of a resource filter. This feature is used in the final middleware
/// of a middleware filter's pipeline to keep the request flow through the rest of the MVC layers.
/// </summary>
internal interface IMiddlewareFilterFeature
{
    ResourceExecutingContext? ResourceExecutingContext { get; }

    ResourceExecutionDelegate? ResourceExecutionDelegate { get; }
}
