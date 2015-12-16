// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Extensions.Globalization;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Enables automatic setting of the culture for <see cref="Http.HttpRequest"/>s based on information
    /// sent by the client in headers and logic provided by the application.
    /// </summary>
    public class RequestLocalizationMiddleware
    {
        private static readonly int MaxCultureFallbackDepth = 5;

        private readonly RequestDelegate _next;
        private readonly RequestLocalizationOptions _options;

        /// <summary>
        /// Creates a new <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options">The <see cref="RequestLocalizationOptions"/> representing the options for the
        /// <see cref="RequestLocalizationMiddleware"/>.</param>
        public RequestLocalizationMiddleware(RequestDelegate next, RequestLocalizationOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options;
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
                    if (providerResultCulture != null)
                    {
                        var cultures = providerResultCulture.Cultures;
                        var uiCultures = providerResultCulture.UICultures;

                        CultureInfo cultureInfo = null;
                        CultureInfo uiCultureInfo = null;
                        if (_options.SupportedCultures != null)
                        {
                            cultureInfo = GetCultureInfo(
                                cultures,
                                _options.SupportedCultures,
                                _options.FallBackToParentCultures,
                                currentDepth: 0);
                        }

                        if (_options.SupportedUICultures != null)
                        {
                            uiCultureInfo = GetCultureInfo(
                                uiCultures,
                                _options.SupportedUICultures,
                                _options.FallBackToParentUICultures,
                                currentDepth: 0);
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
            }

            context.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, winningProvider));

            SetCurrentThreadCulture(requestCulture);

            await _next(context);
        }

        private static void SetCurrentThreadCulture(RequestCulture requestCulture)
        {
#if NET451
            Thread.CurrentThread.CurrentCulture = requestCulture.Culture;
            Thread.CurrentThread.CurrentUICulture = requestCulture.UICulture;
#else
            CultureInfo.CurrentCulture = requestCulture.Culture;
            CultureInfo.CurrentUICulture = requestCulture.UICulture;
#endif
        }

        private static CultureInfo GetCultureInfo(
            IList<string> cultureNames,
            IList<CultureInfo> supportedCultures,
            bool fallbackToAncestorCulture,
            int currentDepth)
        {
            foreach (var cultureName in cultureNames)
            {
                // Allow empty string values as they map to InvariantCulture, whereas null culture values will throw in
                // the CultureInfo ctor
                if (cultureName != null)
                {
                    var cultureInfo = CultureInfoCache.GetCultureInfo(cultureName, supportedCultures);
                    if (cultureInfo != null)
                    {
                        return cultureInfo;
                    }
                }
            }

            if (fallbackToAncestorCulture & currentDepth < MaxCultureFallbackDepth)
            {
                // Walk backwards through the culture list and remove any root cultures (those with no parent)
                for (var i = cultureNames.Count - 1; i >= 0; i--)
                {
                    var cultureName = cultureNames[i];
                    if (cultureName != null)
                    {
                        var lastIndexOfHyphen = cultureName.LastIndexOf('-');
                        if (lastIndexOfHyphen > 0)
                        {
                            // Trim the trailing section from the culture name, e.g. "fr-FR" becomes "fr"
                            cultureNames[i] = cultureName.Substring(0, lastIndexOfHyphen);
                        }
                        else
                        {
                            // The culture had no sections left to trim so remove it from the list of candidates
                            cultureNames.RemoveAt(i);
                        }
                    }
                    else
                    {
                        // Culture name was null so just remove it
                        cultureNames.RemoveAt(i);
                    }
                }

                if (cultureNames.Count > 0)
                {
                    return GetCultureInfo(cultureNames, supportedCultures, fallbackToAncestorCulture, currentDepth + 1);
                }
            }

            return null;
        }
    }
}