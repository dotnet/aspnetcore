// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionFilter"/> that responds to invalid <see cref="ActionContext.ModelState"/>. This filter is
/// added to all types and actions annotated with <see cref="ApiControllerAttribute"/>.
/// See <see cref="ApiBehaviorOptions"/> for ways to configure this filter.
/// </summary>
public partial class ModelStateInvalidFilter : IActionFilter, IOrderedFilter
{
    internal const int FilterOrder = -2000;

    private readonly ApiBehaviorOptions _apiBehaviorOptions;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ModelStateInvalidFilter"/>.
    /// </summary>
    /// <param name="apiBehaviorOptions">The api behavior options.</param>
    /// <param name="logger">The logger.</param>
    public ModelStateInvalidFilter(ApiBehaviorOptions apiBehaviorOptions, ILogger logger)
    {
        _apiBehaviorOptions = apiBehaviorOptions ?? throw new ArgumentNullException(nameof(apiBehaviorOptions));
        if (!_apiBehaviorOptions.SuppressModelStateInvalidFilter && _apiBehaviorOptions.InvalidModelStateResponseFactory == null)
        {
            throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                typeof(ApiBehaviorOptions),
                nameof(ApiBehaviorOptions.InvalidModelStateResponseFactory)));
        }

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
    public int Order => FilterOrder;

    /// <inheritdoc />
    public bool IsReusable => true;

    /// <summary>
    /// Invoked when an action is executed.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutedContext"/>.</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    /// <summary>
    /// Invoked when an action is executing.
    /// </summary>
    /// <param name="context">The <see cref="ActionExecutingContext"/>.</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Result == null && !context.ModelState.IsValid)
        {
            Log.ModelStateInvalidFilterExecuting(_logger);
            context.Result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "The request has model state errors, returning an error response.", EventName = "ModelStateInvalidFilterExecuting")]
        public static partial void ModelStateInvalidFilterExecuting(ILogger logger);
    }
}
