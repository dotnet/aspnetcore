// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentInvokerFactory : IViewComponentInvokerFactory
    {
        private readonly INestedProviderManager<ViewComponentInvokerProviderContext> _providerManager;

        public DefaultViewComponentInvokerFactory(
            INestedProviderManager<ViewComponentInvokerProviderContext> providerManager)
        {
            _providerManager = providerManager;
        }

        public IViewComponentInvoker CreateInstance([NotNull] TypeInfo componentType, object[] args)
        {
            var context = new ViewComponentInvokerProviderContext(componentType, args);
            _providerManager.Invoke(context);
            return context.Result;
        }
    }
}
