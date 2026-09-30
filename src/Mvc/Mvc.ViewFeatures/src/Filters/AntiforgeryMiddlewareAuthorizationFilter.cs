// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Core.Filters;

internal sealed partial class AntiforgeryMiddlewareAuthorizationFilter(ILogger<AntiforgeryMiddlewareAuthorizationFilter> logger) : IAsyncAuthorizationFilter
{
    private readonly ILogger _logger = logger;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Either the token-based AntiforgeryMiddleware or the cross-origin CsrfProtectionMiddleware may have run
        // and recorded its verdict on IAntiforgeryValidationFeature. Reject when either reported the request invalid.
        if (context.HttpContext.Items.ContainsKey(MiddlewareInvokedKeys.Antiforgery) ||
            context.HttpContext.Items.ContainsKey(MiddlewareInvokedKeys.CsrfProtection))
        {
            var antiforgeryValidationFeature = context.HttpContext.Features.Get<IAntiforgeryValidationFeature>();
            if (antiforgeryValidationFeature is { IsValid: false })
            {
                Log.AntiforgeryTokenInvalid(_logger, antiforgeryValidationFeature.Error!.Message, antiforgeryValidationFeature.Error!);
                context.Result = new AntiforgeryValidationFailedResult();
            }
        }

        return Task.CompletedTask;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Antiforgery token validation failed. {Message}", EventName = "AntiforgeryTokenInvalid")]
        public static partial void AntiforgeryTokenInvalid(ILogger logger, string message, Exception exception);
    }
}
