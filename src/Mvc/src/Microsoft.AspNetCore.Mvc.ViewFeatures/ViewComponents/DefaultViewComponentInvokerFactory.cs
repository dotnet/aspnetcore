// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    internal class DefaultViewComponentInvokerFactory : IViewComponentInvokerFactory
    {
        private readonly IViewComponentFactory _viewComponentFactory;
        private readonly ViewComponentInvokerCache _viewComponentInvokerCache;
        private readonly ILogger _logger;
        private readonly DiagnosticListener _diagnosticListener;

        public DefaultViewComponentInvokerFactory(
            IViewComponentFactory viewComponentFactory,
            ViewComponentInvokerCache viewComponentInvokerCache,
            DiagnosticListener diagnosticListener,
            ILoggerFactory loggerFactory)
        {
            if (viewComponentFactory == null)
            {
                throw new ArgumentNullException(nameof(viewComponentFactory));
            }

            if (viewComponentInvokerCache == null)
            {
                throw new ArgumentNullException(nameof(viewComponentInvokerCache));
            }

            if (diagnosticListener == null)
            {
                throw new ArgumentNullException(nameof(diagnosticListener));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _viewComponentFactory = viewComponentFactory;
            _diagnosticListener = diagnosticListener;
            _viewComponentInvokerCache = viewComponentInvokerCache;

            _logger = loggerFactory.CreateLogger<DefaultViewComponentInvoker>();
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
                _viewComponentFactory,
                _viewComponentInvokerCache,
                _diagnosticListener,
                _logger);
        }
    }
}
