// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentInvokerProvider : IViewComponentInvokerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeActivator _typeActivator;
        private readonly IViewComponentActivator _viewComponentActivator;

        public DefaultViewComponentInvokerProvider(
            IServiceProvider serviceProvider,
            ITypeActivator typeActivator,
            IViewComponentActivator viewComponentActivator)
        {
            _serviceProvider = serviceProvider;
            _typeActivator = typeActivator;
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
                    _typeActivator,
                    _viewComponentActivator,
                    context.ComponentType,
                    context.Arguments);

            callNext();
        }
    }
}
