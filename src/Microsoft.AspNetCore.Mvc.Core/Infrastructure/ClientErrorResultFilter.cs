// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class ClientErrorResultFilter : IAlwaysRunResultFilter, IOrderedFilter
    {
        private readonly IDictionary<int, Func<ActionContext, IActionResult>> _clientErrorFactory;
        private readonly ILogger<ClientErrorResultFilter> _logger;

        /// <summary>
        /// Gets the filter order. Defaults to -2000 so that it runs early.
        /// </summary>
        public int Order => -2000;

        public ClientErrorResultFilter(
            ApiBehaviorOptions apiBehaviorOptions,
            ILogger<ClientErrorResultFilter> logger)
        {
            _clientErrorFactory = apiBehaviorOptions?.ClientErrorFactory ?? throw new ArgumentNullException(nameof(apiBehaviorOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Result is IClientErrorActionResult clientErrorActionResult &&
                clientErrorActionResult.StatusCode is int statusCode &&
                _clientErrorFactory.TryGetValue(statusCode, out var factory))
            {
                var result = factory(context);

                _logger.TransformingClientError(context.Result.GetType(), result?.GetType(), statusCode);

                context.Result = factory(context);
            }
        }
    }
}
