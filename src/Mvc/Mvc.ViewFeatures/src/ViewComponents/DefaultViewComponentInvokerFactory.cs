// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

internal sealed class DefaultViewComponentInvokerFactory : IViewComponentInvokerFactory
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
        ArgumentNullException.ThrowIfNull(viewComponentFactory);
        ArgumentNullException.ThrowIfNull(viewComponentInvokerCache);
        ArgumentNullException.ThrowIfNull(diagnosticListener);
        ArgumentNullException.ThrowIfNull(loggerFactory);

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
        ArgumentNullException.ThrowIfNull(context);

        return new DefaultViewComponentInvoker(
            _viewComponentFactory,
            _viewComponentInvokerCache,
            _diagnosticListener,
            _logger);
    }
}
