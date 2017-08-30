// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Localization
{
    /// <summary>
    /// Enables automatic setting of the culture for <see cref="HttpRequest"/>s based on information
    /// sent by the client in headers and logic provided by the application.
    /// </summary>
    public class RequestLocalizationMiddleware
    {
        private static readonly int MaxCultureFallbackDepth = 5;

        private readonly RequestDelegate _next;
        private readonly RequestLocalizationOptions _options;
        private ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options">The <see cref="RequestLocalizationOptions"/> representing the options for the
        /// <see cref="RequestLocalizationMiddleware"/>.</param>
        public RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options.Value;
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
                            EnsureLogger(context);
                            _logger?.UnsupportedCultures(provider.GetType().Name, cultures);
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
                            EnsureLogger(context);
                           _logger?.UnsupportedUICultures(provider.GetType().Name, uiCultures);
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

            await _next(context);
        }

        private void EnsureLogger(HttpContext context)
        {
            _logger = _logger ?? context.RequestServices.GetService<ILogger<RequestLocalizationMiddleware>>();
        }

        private static void SetCurrentThreadCulture(RequestCulture requestCulture)
        {
            CultureInfo.CurrentCulture = requestCulture.Culture;
            CultureInfo.CurrentUICulture = requestCulture.UICulture;
        }

        private static CultureInfo GetCultureInfo(
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

        private static CultureInfo GetCultureInfo(StringSegment name, IList<CultureInfo> supportedCultures)
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

        private static CultureInfo GetCultureInfo(
            StringSegment cultureName,
            IList<CultureInfo> supportedCultures,
            bool fallbackToParentCultures,
            int currentDepth)
        {
            var culture = GetCultureInfo(cultureName, supportedCultures);

            if (culture == null && fallbackToParentCultures && currentDepth < MaxCultureFallbackDepth)
            {
                var lastIndexOfHyphen = cultureName.LastIndexOf('-');

                if (lastIndexOfHyphen > 0)
                {
                    // Trim the trailing section from the culture name, e.g. "fr-FR" becomes "fr"
                    var parentCultureName = cultureName.Subsegment(0, lastIndexOfHyphen);

                    culture = GetCultureInfo(parentCultureName, supportedCultures, fallbackToParentCultures, currentDepth + 1);
                }
            }

            return culture;
        }
    }
}