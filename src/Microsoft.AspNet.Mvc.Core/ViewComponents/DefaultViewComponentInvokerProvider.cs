// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentInvokerProvider : IViewComponentInvokerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeActivatorCache _typeActivatorCache;
        private readonly IViewComponentActivator _viewComponentActivator;

        public DefaultViewComponentInvokerProvider(
            IServiceProvider serviceProvider,
            ITypeActivatorCache typeActivatorCache,
            IViewComponentActivator viewComponentActivator)
        {
            _serviceProvider = serviceProvider;
            _typeActivatorCache = typeActivatorCache;
            _viewComponentActivator = viewComponentActivator;
        }

        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        public void Invoke([NotNull] ViewComponentInvokerProviderContext context, [NotNull] Action callNext)
        {
            context.Result = new DefaultViewComponentInvoker(
                    _serviceProvider,
                    _typeActivatorCache,
                    _viewComponentActivator,
                    context.ComponentType,
                    context.Arguments);

            callNext();
        }
    }
}
