// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    public class DatabaseErrorHandler : IDeveloperPageExceptionFilter
    {
        private readonly ILogger _logger;
        private readonly DatabaseErrorPageOptions _options;

        public DatabaseErrorHandler(ILogger<DatabaseErrorHandler> logger, IOptions<DatabaseErrorPageOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task HandleExceptionAsync(ErrorContext errorContext, Func<ErrorContext, Task> next)
        {
            if (errorContext.Exception is DbException)
            {
                try
                {
                    // Look for DbContext classes registered in the service provider
                    // TODO: Decouple
                    var registeredContexts = errorContext.HttpContext.RequestServices.GetServices<DbContextOptions>()
                        .Select(o => o.ContextType);

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
                                Model = new DatabaseErrorPageModel(errorContext.Exception, contextDetails, _options)
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

                await next(errorContext);
            }
            else
            {
                await next(errorContext);
            }
        }
    }
}
