// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Localization
{
    /// <summary>
    /// Enables automatic setting of the culture for <see cref="HttpRequest"/>s based on information
    /// sent by the client in headers and logic provided by the application.
    /// </summary>
    public class RequestLocalizationMiddleware
    {
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
        [ActivatorUtilitiesConstructor]
        public RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory?.CreateLogger<RequestLocalizationMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _options = options.Value;
        }

        /// <summary>
        /// Creates a new <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options">The <see cref="RequestLocalizationOptions"/> representing the options for the
        /// <see cref="RequestLocalizationMiddleware"/>.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version. Use RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options, ILoggerFactory loggerFactory) instead")]
        public RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options)
               : this(next, options, NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var requestCulture = _options.DefaultRequestCulture;

            IRequestCultureProvider winningProvider = null;

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
                    CultureInfo cultureInfo = null;
                    CultureInfo uiCultureInfo = null;

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

                    if (cultureInfo == null && uiCultureInfo != null)
                    {
                        cultureInfo = _options.DefaultRequestCulture.Culture;
                    }

                    if (cultureInfo != null && uiCultureInfo == null)
                    {
                        uiCultureInfo = _options.DefaultRequestCulture.UICulture;
                    }

                    var result = new RequestCulture(cultureInfo, uiCultureInfo);

                    if (result != null)
                    {
                        requestCulture = result;
                        winningProvider = provider;
                        break;
                    }
                }
            }

            context.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, winningProvider));

            SetCurrentThreadCulture(requestCulture);

            if (_options.ApplyCurrentCultureToResponseHeaders)
            {
                context.Response.Headers.Add(HeaderNames.ContentLanguage, requestCulture.UICulture.Name);
            }

            await _next(context);
        }

        private static void SetCurrentThreadCulture(RequestCulture requestCulture)
        {
            CultureInfo.CurrentCulture = requestCulture.Culture;
            CultureInfo.CurrentUICulture = requestCulture.UICulture;
        }

        private static CultureInfo GetCultureInfo(
            IList<StringSegment> cultureNames,
            IEnumerable<CultureInfo> supportedCultures,
            bool fallbackToParentCultures)
        {
            foreach (var cultureName in GetValidNonNullableCultures())
            {
                var cultureInfo = supportedCultures?
                    .FirstOrDefault(c => StringSegment.Equals(c.Name, cultureName, StringComparison.OrdinalIgnoreCase))
                    ?? null;

                if (cultureInfo == null)
                {
                    if (fallbackToParentCultures)
                    {
                        var fallbackCulture = CultureInfo.GetCultureInfo(cultureName.Value);

                        while (fallbackCulture != fallbackCulture.Parent)
                        {
                            fallbackCulture = fallbackCulture.Parent;

                            if (supportedCultures.Contains(fallbackCulture))
                            {
                                return fallbackCulture;
                            }
                        }

                        if (supportedCultures.Contains(fallbackCulture))
                        {
                            return fallbackCulture;
                        }
                    }
                }
                else
                {
                    return cultureInfo;
                }
            }

            return null;

            IEnumerable<StringSegment> GetValidNonNullableCultures()
            {
                var validCultures = new List<StringSegment>();

                foreach (var name in cultureNames)
                {
                    try
                    {
                        if (name == null)
                        {
                            continue;
                        }

                        var culture = CultureInfo.GetCultureInfo(name.Value);
                        if (!culture.DisplayName.StartsWith("Unknown Locale"))
                        {
                            validCultures.Add(name);
                        }

                    }
                    catch (CultureNotFoundException)
                    {

                    }
                }

                return validCultures;
            }
        }
    }
}
