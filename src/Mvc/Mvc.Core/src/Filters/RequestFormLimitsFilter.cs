// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that configures <see cref="FormOptions"/> for the current request.
/// </summary>
internal partial class RequestFormLimitsFilter : IAuthorizationFilter, IRequestFormLimitsPolicy
{
    private readonly ILogger _logger;

    public RequestFormLimitsFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RequestFormLimitsFilter>();
    }

    public FormOptions FormOptions { get; set; } = default!;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var effectivePolicy = context.FindEffectivePolicy<IRequestFormLimitsPolicy>();
        if (effectivePolicy != null && effectivePolicy != this)
        {
            Log.NotMostEffectiveFilter(_logger, GetType(), effectivePolicy.GetType(), typeof(IRequestFormLimitsPolicy));
            return;
        }

        var features = context.HttpContext.Features;
        var formFeature = features.Get<IFormFeature>();

        if (formFeature == null || formFeature.Form == null)
        {
            // Request form has not been read yet, so set the limits
            features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, FormOptions));
            Log.AppliedRequestFormLimits(_logger);
        }
        else
        {
            Log.CannotApplyRequestFormLimits(_logger);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Unable to apply configured form options since the request form has already been read.", EventName = "CannotApplyRequestFormLimits")]
        public static partial void CannotApplyRequestFormLimits(ILogger logger);

        [LoggerMessage(2, LogLevel.Debug, "Applied the configured form options on the current request.", EventName = "AppliedRequestFormLimits")]
        public static partial void AppliedRequestFormLimits(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "Execution of filter {OverriddenFilter} is preempted by filter {OverridingFilter} which is the most effective filter implementing policy {FilterPolicy}.", EventName = "NotMostEffectiveFilter")]
        public static partial void NotMostEffectiveFilter(ILogger logger, Type overriddenFilter, Type overridingFilter, Type filterPolicy);
    }
}
