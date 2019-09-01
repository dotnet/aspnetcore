// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ForbiddenObjectResultExecutor : IActionResultExecutor<ForbiddenObjectResult>
    {
        private readonly ILogger<ForbiddenObjectResult> _logger;

        public ForbiddenObjectResultExecutor(ILogger<ForbiddenObjectResult> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public virtual async Task ExecuteAsync(ActionContext context, ForbiddenObjectResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            _logger.ForbidResultExecuting(result.AuthenticationSchemes);

            if (result.AuthenticationSchemes != null && result.AuthenticationSchemes.Count > 0)
            {
                for (var i = 0; i < result.AuthenticationSchemes.Count; i++)
                {
                    await context.HttpContext.ForbidAsync(result.AuthenticationSchemes[i], result.Properties);
                }
            }
            else
            {
                await context.HttpContext.ForbidAsync(result.Properties);
            }
        }
    }
}
