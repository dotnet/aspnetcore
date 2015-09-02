// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Actions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentInvokerFactory : IViewComponentInvokerFactory
    {
        private readonly ITypeActivatorCache _typeActivatorCache;
        private readonly IViewComponentActivator _viewComponentActivator;

        public DefaultViewComponentInvokerFactory(
            ITypeActivatorCache typeActivatorCache,
            IViewComponentActivator viewComponentActivator)
        {
            _typeActivatorCache = typeActivatorCache;
            _viewComponentActivator = viewComponentActivator;
        }

        /// <inheritdoc />
        // We don't currently make use of the descriptor or the arguments here (they are available on the context).
        // We might do this some day to cache which method we select, so resist the urge to 'clean' this without
        // considering that possibility.
        public IViewComponentInvoker CreateInstance([NotNull] ViewComponentContext context)
        {
            return new DefaultViewComponentInvoker(
                _typeActivatorCache,
                _viewComponentActivator);
        }
    }
}
