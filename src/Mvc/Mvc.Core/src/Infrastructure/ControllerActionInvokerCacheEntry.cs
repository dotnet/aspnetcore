// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class ControllerActionInvokerCacheEntry
{
    internal ControllerActionInvokerCacheEntry(
        FilterItem[] cachedFilters,
        Func<ControllerContext, object> controllerFactory,
        Func<ControllerContext, object, ValueTask>? controllerReleaser,
        ControllerBinderDelegate? controllerBinderDelegate,
        ObjectMethodExecutor objectMethodExecutor,
        ActionMethodExecutor actionMethodExecutor,
        ActionMethodExecutor innerActionMethodExecutor)
    {
        ControllerFactory = controllerFactory;
        ControllerReleaser = controllerReleaser;
        ControllerBinderDelegate = controllerBinderDelegate;
        CachedFilters = cachedFilters;
        ObjectMethodExecutor = objectMethodExecutor;
        ActionMethodExecutor = actionMethodExecutor;
        InnerActionMethodExecutor = innerActionMethodExecutor;
    }

    public FilterItem[] CachedFilters { get; }

    public Func<ControllerContext, object> ControllerFactory { get; }

    public Func<ControllerContext, object, ValueTask>? ControllerReleaser { get; }

    public ControllerBinderDelegate? ControllerBinderDelegate { get; }

    internal ObjectMethodExecutor ObjectMethodExecutor { get; }

    // This includes the execution of the filter delegate (if there's a filter)
    internal ActionMethodExecutor ActionMethodExecutor { get; }

    // This is called inside of the filter delegate
    internal ActionMethodExecutor InnerActionMethodExecutor { get; }
}
