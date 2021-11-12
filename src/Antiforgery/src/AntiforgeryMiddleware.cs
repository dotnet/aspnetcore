// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Abstractions.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed partial class AntiforgeryMiddleware
{
    private readonly IAntiforgery _antiforgery;
    private readonly RequestDelegate _next;
    private readonly ILogger<AntiforgeryMiddleware> _logger;

    public AntiforgeryMiddleware(IAntiforgery antiforgery, RequestDelegate next, ILogger<AntiforgeryMiddleware> logger)
    {
        _antiforgery = antiforgery;
        _next = next;
        _logger = logger;
    }

    public Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return _next(context);
        }

        var antiforgeryMetadata = endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>();
        if (antiforgeryMetadata is null)
        {
            Log.NoAntiforgeryMetadataFound(_logger);
            return _next(context);
        }
        
        if (antiforgeryMetadata is not IValidateAntiforgeryMetadata validateAntiforgeryMetadata)
        {
            Log.IgnoreAntiforgeryMetadataFound(_logger);
            return _next(context);
        }

        if (_antiforgery is DefaultAntiforgery defaultAntiforgery)
        {
            var valueTask = defaultAntiforgery.TryValidateAsync(context, validateAntiforgeryMetadata.ValidateIdempotentRequests);
            if (valueTask.IsCompletedSuccessfully)
            {
                var (success, message) = valueTask.GetAwaiter().GetResult();
                if (success)
                {
                    Log.AntiforgeryValidationSucceeded(_logger);
                    return _next(context);
                }
                else
                {
                    Log.AntiforgeryValidationFailed(_logger, message);
                    return WriteAntiforgeryInvalidResponseAsync(context, message);
                }
            }

            return TryValidateAsyncAwaited(context, valueTask);
        }
        else
        {
            return ValidateNonDefaultAntiforgery(context);
        }
    }

    private async Task TryValidateAsyncAwaited(HttpContext context, ValueTask<(bool success, string? message)> tryValidateTask)
    {
        var (success, message) = await tryValidateTask;
        if (success)
        {
            Log.AntiforgeryValidationSucceeded(_logger);
            await _next(context);
        }
        else
        {
            Log.AntiforgeryValidationFailed(_logger, message);
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Antiforgery validation failed",
                Detail = message,
            });
        }
    }

    private async Task ValidateNonDefaultAntiforgery(HttpContext context)
    {
        if (await _antiforgery.IsRequestValidAsync(context))
        {
            Log.AntiforgeryValidationSucceeded(_logger);
            await _next(context);
        }
        else
        { 
            Log.AntiforgeryValidationFailed(_logger, message: null);
            await WriteAntiforgeryInvalidResponseAsync(context, message: null);
        }
    }

    private static Task WriteAntiforgeryInvalidResponseAsync(HttpContext context, string? message)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Antiforgery validation failed",
            Detail = message,
        });
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "No antiforgery metadata found on the endpoint.", EventName = "NoAntiforgeryMetadataFound")]
        public static partial void NoAntiforgeryMetadataFound(ILogger logger);

        [LoggerMessage(2, LogLevel.Debug, $"Antiforgery validation suppressed on endpoint because {nameof(IValidateAntiforgeryMetadata)} was not found.", EventName = "IgnoreAntiforgeryMetadataFound")]
        public static partial void IgnoreAntiforgeryMetadataFound(ILogger logger);

        [LoggerMessage(3, LogLevel.Debug, "Antiforgery validation completed successfully.", EventName = "AntiforgeryValidationSucceeded")]
        public static partial void AntiforgeryValidationSucceeded(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "Antiforgery validation failed with message '{message}'.", EventName = "AntiforgeryValidationFailed")]
        public static partial void AntiforgeryValidationFailed(ILogger logger, string? message);
    }
}
