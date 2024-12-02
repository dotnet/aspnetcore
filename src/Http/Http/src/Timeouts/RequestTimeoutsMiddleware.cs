// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Timeouts;

internal sealed class RequestTimeoutsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICancellationTokenLinker _cancellationTokenProvider;
    private readonly ILogger<RequestTimeoutsMiddleware> _logger;
    private readonly IOptionsMonitor<RequestTimeoutOptions> _options;

    public RequestTimeoutsMiddleware(
        RequestDelegate next,
        ICancellationTokenLinker cancellationTokenProvider,
        ILogger<RequestTimeoutsMiddleware> logger,
        IOptionsMonitor<RequestTimeoutOptions> options)
    {
        _next = next;
        _cancellationTokenProvider = cancellationTokenProvider;
        _logger = logger;
        _options = options;
    }

    public Task Invoke(HttpContext context)
    {
        if (Debugger.IsAttached)
        {
            return _next(context);
        }

        var endpoint = context.GetEndpoint();
        var timeoutMetadata = endpoint?.Metadata.GetMetadata<RequestTimeoutAttribute>();
        var policyMetadata = endpoint?.Metadata.GetMetadata<RequestTimeoutPolicy>();

        var options = _options.CurrentValue;

        if (timeoutMetadata is null && policyMetadata?.Timeout is null && options.DefaultPolicy?.Timeout is null)
        {
            return _next(context);
        }

        var disableMetadata = endpoint?.Metadata.GetMetadata<DisableRequestTimeoutAttribute>();
        if (disableMetadata is not null)
        {
            return _next(context);
        }

        RequestTimeoutPolicy? selectedPolicy = null;
        TimeSpan? timeSpan = null;
        if (policyMetadata is not null)
        {
            selectedPolicy = policyMetadata;
        }
        else if (timeoutMetadata is not null)
        {
            if (timeoutMetadata.Timeout is not null)
            {
                timeSpan = timeoutMetadata.Timeout.Value;
            }
            else
            {
                if (options.Policies.TryGetValue(timeoutMetadata.PolicyName!, out var policy))
                {
                    selectedPolicy = policy;
                }
                else
                {
                    throw new InvalidOperationException($"The requested timeout policy '{timeoutMetadata.PolicyName}' is not available.");
                }
            }
        }
        else
        {
            selectedPolicy = options.DefaultPolicy;
        }

        timeSpan ??= selectedPolicy?.Timeout;

        if (timeSpan is null || timeSpan == Timeout.InfiniteTimeSpan)
        {
            return _next(context);
        }

        if (context.RequestAborted.IsCancellationRequested)
        {
            return _next(context);
        }

        return SetTimeoutAsync();

        async Task SetTimeoutAsync()
        {
            var originalToken = context.RequestAborted;
            var (linkedCts, timeoutCts) = _cancellationTokenProvider.GetLinkedCancellationTokenSource(context, originalToken, timeSpan.Value);

            try
            {
                var feature = new HttpRequestTimeoutFeature(timeoutCts);
                context.Features.Set<IHttpRequestTimeoutFeature>(feature);

                context.RequestAborted = linkedCts.Token;
                await _next(context);
            }
            catch (OperationCanceledException operationCanceledException)
            when (linkedCts.IsCancellationRequested && !originalToken.IsCancellationRequested)
            {
                if (context.Response.HasStarted)
                {
                    // We can't produce a response, or it wasn't our timeout that caused this.
                    throw;
                }

                _logger.TimeoutExceptionHandled(operationCanceledException);

                context.Response.Clear();

                context.Response.StatusCode = selectedPolicy?.TimeoutStatusCode ?? StatusCodes.Status504GatewayTimeout;

                if (selectedPolicy?.WriteTimeoutResponse is not null)
                {
                    await selectedPolicy.WriteTimeoutResponse(context);
                }
            }
            finally
            {
                linkedCts.Dispose();
                context.RequestAborted = originalToken;
                context.Features.Set<IHttpRequestTimeoutFeature>(null);
            }
        }
    }
}
