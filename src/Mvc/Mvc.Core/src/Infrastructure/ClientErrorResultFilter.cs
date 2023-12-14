// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed partial class ClientErrorResultFilter : IAlwaysRunResultFilter, IOrderedFilter
{
    internal const int FilterOrder = -2000;
    private readonly IClientErrorFactory _clientErrorFactory;
    private readonly ILogger<ClientErrorResultFilter> _logger;

    public ClientErrorResultFilter(
        IClientErrorFactory clientErrorFactory,
        ILogger<ClientErrorResultFilter> logger)
    {
        _clientErrorFactory = clientErrorFactory ?? throw new ArgumentNullException(nameof(clientErrorFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the filter order. Defaults to -2000 so that it runs early.
    /// </summary>
    public int Order => FilterOrder;

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!(context.Result is IClientErrorActionResult clientError))
        {
            return;
        }

        // We do not have an upper bound on the allowed status code. This allows this filter to be used
        // for 5xx and later status codes.
        if (clientError.StatusCode < 400)
        {
            return;
        }

        var result = _clientErrorFactory.GetClientError(context, clientError);
        if (result == null)
        {
            return;
        }

        Log.TransformingClientError(_logger, context.Result.GetType(), result.GetType(), clientError.StatusCode);
        context.Result = result;
    }

    private static partial class Log
    {
        [LoggerMessage(49, LogLevel.Trace, "Replacing {InitialActionResultType} with status code {StatusCode} with {ReplacedActionResultType}.", EventName = "ClientErrorResultFilter")]
        public static partial void TransformingClientError(ILogger logger, Type initialActionResultType, Type replacedActionResultType, int? statusCode);
    }
}
