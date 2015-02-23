// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentInvokerFactory : IViewComponentInvokerFactory
    {
        private readonly IViewComponentInvokerProvider[] _providers;

        public DefaultViewComponentInvokerFactory(
            IEnumerable<IViewComponentInvokerProvider> providers)
        {
            _providers = providers.OrderBy(item => item.Order).ToArray();
        }

        public IViewComponentInvoker CreateInstance([NotNull] TypeInfo componentType, object[] args)
        {
            var context = new ViewComponentInvokerProviderContext(componentType, args);

            foreach (var provider in _providers)
            {
                provider.OnProvidersExecuting(context);
            }

            for (var i = _providers.Length - 1; i >= 0; i--)
            {
                _providers[i].OnProvidersExecuted(context);
            }

            return context.Result;
        }
    }
}
