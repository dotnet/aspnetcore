// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvokerCacheEntry
    {
        internal ControllerActionInvokerCacheEntry(
            FilterItem[] cachedFilters,
            Func<ControllerContext, object> controllerFactory,
            Action<ControllerContext, object> controllerReleaser,
            ControllerBinderDelegate controllerBinderDelegate,
            ObjectMethodExecutor objectMethodExecutor,
            ActionMethodExecutor actionMethodExecutor)
        {
            ControllerFactory = controllerFactory;
            ControllerReleaser = controllerReleaser;
            ControllerBinderDelegate = controllerBinderDelegate;
            CachedFilters = cachedFilters;
            ObjectMethodExecutor = objectMethodExecutor;
            ActionMethodExecutor = actionMethodExecutor;
        }

        public FilterItem[] CachedFilters { get; }

        public Func<ControllerContext, object> ControllerFactory { get; }

        public Action<ControllerContext, object> ControllerReleaser { get; }

        public ControllerBinderDelegate ControllerBinderDelegate { get; }

        internal ObjectMethodExecutor ObjectMethodExecutor { get; }

        internal ActionMethodExecutor ActionMethodExecutor { get; }
    }
}
