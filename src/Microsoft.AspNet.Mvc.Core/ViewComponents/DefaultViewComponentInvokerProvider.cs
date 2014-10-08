// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentInvokerProvider : IViewComponentInvokerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewComponentActivator _viewComponentActivator;

        public DefaultViewComponentInvokerProvider(IServiceProvider serviceProvider,
            IViewComponentActivator viewComponentActivator)
        {
            _serviceProvider = serviceProvider;
            _viewComponentActivator = viewComponentActivator;
        }

        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        public void Invoke([NotNull] ViewComponentInvokerProviderContext context, [NotNull] Action callNext)
        {
            context.Result =
                new DefaultViewComponentInvoker(
                    _serviceProvider, _viewComponentActivator, context.ComponentType, context.Arguments);
            callNext();
        }
    }
}
