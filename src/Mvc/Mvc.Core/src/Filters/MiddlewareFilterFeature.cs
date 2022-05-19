// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class MiddlewareFilterFeature : IMiddlewareFilterFeature
{
    public ResourceExecutingContext? ResourceExecutingContext { get; set; }

    public ResourceExecutionDelegate? ResourceExecutionDelegate { get; set; }
}
