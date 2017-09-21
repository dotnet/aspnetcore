// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// A <see cref="IActionFilter"/> that responds to invalid <see cref="ActionContext.ModelState"/>. This filter is
    /// added to all types and actions annotated with <see cref="ApiControllerAttribute"/>.
    /// See <see cref="ApiBehaviorOptions"/> for ways to configure this filter.
    /// </summary>
    public class ModelStateInvalidFilter : IActionFilter, IOrderedFilter
    {
        private readonly ApiBehaviorOptions _apiBehaviorOptions;
        private readonly ILogger _logger;

        public ModelStateInvalidFilter(ApiBehaviorOptions apiBehaviorOptions, ILogger logger)
        {
            _apiBehaviorOptions = apiBehaviorOptions ?? throw new ArgumentNullException(nameof(apiBehaviorOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the order value for determining the order of execution of filters. Filters execute in
        /// ascending numeric value of the <see cref="Order"/> property.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Filters are executed in a sequence determined by an ascending sort of the <see cref="Order"/> property.
        /// </para>
        /// <para>
        /// The default Order for this attribute is -2000 so that it runs early in the pipeline.
        /// </para>
        /// <para>
        /// Look at <see cref="IOrderedFilter.Order"/> for more detailed info.
        /// </para>
        /// </remarks>
        public int Order => -2000;

        /// <inheritdoc />
        public bool IsReusable => true;

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Result == null && !context.ModelState.IsValid)
            {
                _logger.ModelStateInvalidFilterExecuting();
                context.Result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context);
            }
        }
    }
}
