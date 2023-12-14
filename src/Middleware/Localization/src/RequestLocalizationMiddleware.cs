// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Localization;

/// <summary>
/// Enables automatic setting of the culture for <see cref="HttpRequest"/>s based on information
/// sent by the client in headers and logic provided by the application.
/// </summary>
public class RequestLocalizationMiddleware
{
    private const int MaxCultureFallbackDepth = 5;

    private readonly RequestDelegate _next;
    private readonly RequestLocalizationOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new <see cref="RequestLocalizationMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="options">The <see cref="RequestLocalizationOptions"/> representing the options for the
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
    /// <see cref="RequestLocalizationMiddleware"/>.</param>
    public RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = loggerFactory?.CreateLogger<RequestLocalizationMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options.Value;
    }

    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
    public async Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestCulture = _options.DefaultRequestCulture;

        IRequestCultureProvider? winningProvider = null;

        if (_options.RequestCultureProviders != null)
        {
            foreach (var provider in _options.RequestCultureProviders)
            {
                var providerResultCulture = await provider.DetermineProviderCultureResult(context);
                if (providerResultCulture == null)
                {
                    continue;
                }
                var cultures = providerResultCulture.Cultures;
                var uiCultures = providerResultCulture.UICultures;

                CultureInfo? cultureInfo = null;
                CultureInfo? uiCultureInfo = null;
                if (_options.SupportedCultures != null)
                {
                    cultureInfo = GetCultureInfo(
                        cultures,
                        _options.SupportedCultures,
                        _options.FallBackToParentCultures);

                    if (cultureInfo == null)
                    {
                        _logger.UnsupportedCultures(provider.GetType().Name, cultures);
                    }
                }

                if (_options.SupportedUICultures != null)
                {
                    uiCultureInfo = GetCultureInfo(
                        uiCultures,
                        _options.SupportedUICultures,
                        _options.FallBackToParentUICultures);

                    if (uiCultureInfo == null)
                    {
                        _logger.UnsupportedUICultures(provider.GetType().Name, uiCultures);
                    }
                }

                if (cultureInfo == null && uiCultureInfo == null)
                {
                    continue;
                }

                cultureInfo ??= _options.DefaultRequestCulture.Culture;
                uiCultureInfo ??= _options.DefaultRequestCulture.UICulture;

                var result = new RequestCulture(cultureInfo, uiCultureInfo);
                requestCulture = result;
                winningProvider = provider;
                break;
            }
        }

        context.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, winningProvider));

        SetCurrentThreadCulture(requestCulture);

        if (_options.ApplyCurrentCultureToResponseHeaders)
        {
            var headers = context.Response.Headers;
            headers.ContentLanguage = requestCulture.UICulture.Name;
        }

        await _next(context);
    }

    private static void SetCurrentThreadCulture(RequestCulture requestCulture)
    {
        CultureInfo.CurrentCulture = requestCulture.Culture;
        CultureInfo.CurrentUICulture = requestCulture.UICulture;
    }

    private static CultureInfo? GetCultureInfo(
        IList<StringSegment> cultureNames,
        IList<CultureInfo> supportedCultures,
        bool fallbackToParentCultures)
    {
        foreach (var cultureName in cultureNames)
        {
            // Allow empty string values as they map to InvariantCulture, whereas null culture values will throw in
            // the CultureInfo ctor
            if (cultureName != null)
            {
                var cultureInfo = GetCultureInfo(cultureName, supportedCultures, fallbackToParentCultures, currentDepth: 0);
                if (cultureInfo != null)
                {
                    return cultureInfo;
                }
            }
        }

        return null;
    }

    private static CultureInfo? GetCultureInfo(
        StringSegment cultureName,
        IList<CultureInfo>? supportedCultures,
        bool fallbackToParentCultures,
        int currentDepth)
    {
        // If the cultureName is an empty string there
        // is no chance we can resolve the culture info.
        if (cultureName.Equals(string.Empty))
        {
            return null;
        }

        var culture = GetCultureInfo(cultureName, supportedCultures);

        if (culture == null && fallbackToParentCultures && currentDepth < MaxCultureFallbackDepth)
        {
            try
            {
                culture = CultureInfo.GetCultureInfo(cultureName.ToString());

                culture = GetCultureInfo(culture.Parent.Name, supportedCultures, fallbackToParentCultures, currentDepth + 1);
            }
            catch (CultureNotFoundException)
            {
            }
        }

        return culture;
    }

    private static CultureInfo? GetCultureInfo(StringSegment name, IList<CultureInfo>? supportedCultures)
    {
        // Allow only known culture names as this API is called with input from users (HTTP requests) and
        // creating CultureInfo objects is expensive and we don't want it to throw either.
        if (name == null || supportedCultures == null)
        {
            return null;
        }

        var culture = supportedCultures.FirstOrDefault(
            supportedCulture => StringSegment.Equals(supportedCulture.Name, name, StringComparison.OrdinalIgnoreCase));

        if (culture == null)
        {
            return null;
        }

        return CultureInfo.ReadOnly(culture);
    }
}
