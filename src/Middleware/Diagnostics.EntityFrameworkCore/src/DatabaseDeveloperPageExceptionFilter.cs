// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;

/// <summary>
/// An <see cref="IDeveloperPageExceptionFilter"/> that captures database-related exceptions that can be resolved by using Entity Framework migrations.
/// When these exceptions occur, an HTML response with details about possible actions to resolve the issue is generated.
/// </summary>
/// <remarks>
/// This should only be enabled in the Development environment.
/// </remarks>
[RequiresDynamicCode("DbContext migrations operations are not supported with NativeAOT")]
public sealed class DatabaseDeveloperPageExceptionFilter : IDeveloperPageExceptionFilter
{
    private readonly ILogger _logger;
    private readonly DatabaseErrorPageOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="DatabaseDeveloperPageExceptionFilter"/>.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="options">The <see cref="IOptions{DatabaseErrorPageOptions}"/>.</param>
    public DatabaseDeveloperPageExceptionFilter(ILogger<DatabaseDeveloperPageExceptionFilter> logger, IOptions<DatabaseErrorPageOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Handle <see cref="DbException"/> errors and output an HTML response with additional details.
    /// </summary>
    /// <inheritdoc />
    public async Task HandleExceptionAsync(ErrorContext errorContext, Func<ErrorContext, Task> next)
    {
        var dbException = errorContext.Exception as DbException
              ?? errorContext.Exception?.InnerException as DbException;

        if (dbException == null)
        {
            await next(errorContext);
            return;
        }

        try
        {
            // Look for DbContext classes registered in the service provider
            var registeredContexts = errorContext.HttpContext.RequestServices.GetServices<DbContextOptions>()
                .Select(o => o.ContextType)
                .Distinct(); // Workaround for https://github.com/dotnet/efcore/issues/22341

            if (registeredContexts.Any())
            {
                var contextDetails = new List<DatabaseContextDetails>();

                foreach (var registeredContext in registeredContexts)
                {
                    var details = await errorContext.HttpContext.GetContextDetailsAsync(registeredContext, _logger);

                    if (details != null)
                    {
                        contextDetails.Add(details);
                    }
                }

                if (contextDetails.Any(c => c.PendingModelChanges || c.PendingMigrations.Any()))
                {
                    var page = new DatabaseErrorPage
                    {
                        Model = new DatabaseErrorPageModel(dbException, contextDetails, _options, errorContext.HttpContext.Request.PathBase)
                    };

                    await page.ExecuteAsync(errorContext.HttpContext);
                    return;
                }
            }
        }
        catch (Exception e)
        {
            _logger.DatabaseErrorPageMiddlewareException(e);
        }

        // Error could not be handled
        var response = errorContext.HttpContext.Response;

        if (response.HasStarted)
        {
            _logger.ResponseStartedDatabaseDeveloperPageExceptionFilter();
            return;
        }

        // Try the next filter
        response.Clear();
        response.StatusCode = 500;
        await next(errorContext);
    }
}
