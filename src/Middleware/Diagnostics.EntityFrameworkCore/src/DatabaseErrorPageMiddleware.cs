// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;

/// <summary>
///     Captures synchronous and asynchronous database related exceptions from the pipeline that may be resolved using Entity Framework
///     migrations. When these exceptions occur an HTML response with details of possible actions to resolve the issue is generated.
/// </summary>
[RequiresDynamicCode("DbContext migrations operations are not supported with NativeAOT")]
public class DatabaseErrorPageMiddleware : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
{
    private static readonly AsyncLocal<DiagnosticHolder> _localDiagnostic = new AsyncLocal<DiagnosticHolder>();

    private sealed class DiagnosticHolder
    {
        public void Hold(Exception exception, Type contextType)
        {
            Exception = exception;
            ContextType = contextType;
        }

        public Exception? Exception { get; private set; }
        public Type? ContextType { get; private set; }
    }

    private readonly RequestDelegate _next;
    private readonly DatabaseErrorPageOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseErrorPageMiddleware" /> class
    /// </summary>
    /// <param name="next">Delegate to execute the next piece of middleware in the request pipeline.</param>
    /// <param name="loggerFactory">
    ///     The <see cref="ILoggerFactory" /> for the application. This middleware both produces logging messages and
    ///     consumes them to detect database related exception.
    /// </param>
    /// <param name="options">The options to control what information is displayed on the error page.</param>
    [Obsolete("This is obsolete and will be removed in a future version. Use DatabaseDeveloperPageExceptionFilter instead, see documentation at https://aka.ms/DatabaseDeveloperPageExceptionFilter.")]
    public DatabaseErrorPageMiddleware(
        RequestDelegate next,
        ILoggerFactory loggerFactory,
        IOptions<DatabaseErrorPageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
        _logger = loggerFactory.CreateLogger<DatabaseErrorPageMiddleware>();

        // Note: this currently leaks if the server hosting this middleware is disposed.
        // See aspnet/Home #2825
        DiagnosticListener.AllListeners.Subscribe(this);
    }

    /// <summary>
    ///     Process an individual request.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task Invoke(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        try
        {
            // Because CallContext is cloned at each async operation we cannot
            // lazily create the error object when an error is encountered, otherwise
            // it will not be available to code outside of the current async context.
            // We create it ahead of time so that any cloning just clones the reference
            // to the object that will hold any errors.

            _localDiagnostic.Value = new DiagnosticHolder();

            await _next(httpContext);
        }
        catch (Exception exception)
        {
            try
            {
                if (ShouldDisplayErrorPage(exception))
                {
                    var contextType = _localDiagnostic.Value!.ContextType;
                    var details = await httpContext.GetContextDetailsAsync(contextType!, _logger);

                    if (details != null && (details.PendingModelChanges || details.PendingMigrations.Any()))
                    {
                        var page = new DatabaseErrorPage
                        {
                            Model = new DatabaseErrorPageModel(exception, new DatabaseContextDetails[] { details }, _options, httpContext.Request.PathBase)
                        };

                        await page.ExecuteAsync(httpContext);

                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.DatabaseErrorPageMiddlewareException(e);
            }

            throw;
        }
    }

    private bool ShouldDisplayErrorPage(Exception exception)
    {
        _logger.AttemptingToMatchException(exception.GetType());

        var lastRecordedException = _localDiagnostic.Value!.Exception;

        if (lastRecordedException == null)
        {
            _logger.NoRecordedException();

            return false;
        }

        var match = false;

        for (var e = exception; e != null && !match; e = e.InnerException)
        {
            match = lastRecordedException == e;
        }

        if (!match)
        {
            _logger.NoMatch();

            return false;
        }

        _logger.Matched();

        return true;
    }

    void IObserver<DiagnosticListener>.OnNext(DiagnosticListener diagnosticListener)
    {
        if (diagnosticListener.Name == DbLoggerCategory.Name)
        {
            diagnosticListener.Subscribe(this);
        }
    }

    void IObserver<KeyValuePair<string, object?>>.OnNext(KeyValuePair<string, object?> keyValuePair)
    {
        switch (keyValuePair.Value)
        {
            // NB: _localDiagnostic.Value can be null when this middleware has been leaked.

            case DbContextErrorEventData contextErrorEventData:
                {
                    _localDiagnostic.Value?.Hold(contextErrorEventData.Exception, contextErrorEventData.Context!.GetType());

                    break;
                }
            case DbContextTypeErrorEventData contextTypeErrorEventData:
                {
                    _localDiagnostic.Value?.Hold(contextTypeErrorEventData.Exception, contextTypeErrorEventData.ContextType);

                    break;
                }
        }
    }

    void IObserver<DiagnosticListener>.OnCompleted()
    {
    }

    void IObserver<DiagnosticListener>.OnError(Exception error)
    {
    }

    void IObserver<KeyValuePair<string, object?>>.OnCompleted()
    {
    }

    void IObserver<KeyValuePair<string, object?>>.OnError(Exception error)
    {
    }
}
