// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class ControllerActionInvokerCacheEntry
    {
        internal ControllerActionInvokerCacheEntry(
            FilterItem[] cachedFilters,
            Func<ControllerContext, object> controllerFactory,
            Func<ControllerContext, object, ValueTask> controllerReleaser,
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

        public Func<ControllerContext, object, ValueTask> ControllerReleaser { get; }

        public ControllerBinderDelegate ControllerBinderDelegate { get; }

        internal ObjectMethodExecutor ObjectMethodExecutor { get; }

        internal ActionMethodExecutor ActionMethodExecutor { get; }
    }
}
