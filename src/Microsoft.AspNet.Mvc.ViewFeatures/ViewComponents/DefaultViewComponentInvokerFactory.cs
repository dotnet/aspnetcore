// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentInvokerFactory : IViewComponentInvokerFactory
    {
        private readonly ITypeActivatorCache _typeActivatorCache;
        private readonly IViewComponentActivator _viewComponentActivator;
        private readonly DiagnosticSource _diagnosticSource;

        public DefaultViewComponentInvokerFactory(
            ITypeActivatorCache typeActivatorCache,
            IViewComponentActivator viewComponentActivator,
            DiagnosticSource diagnosticSource)
        {
            _typeActivatorCache = typeActivatorCache;
            _viewComponentActivator = viewComponentActivator;
            _diagnosticSource = diagnosticSource;
        }

        /// <inheritdoc />
        // We don't currently make use of the descriptor or the arguments here (they are available on the context).
        // We might do this some day to cache which method we select, so resist the urge to 'clean' this without
        // considering that possibility.
        public IViewComponentInvoker CreateInstance(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new DefaultViewComponentInvoker(
                _typeActivatorCache,
                _viewComponentActivator,
                _diagnosticSource);
        }
    }
}
