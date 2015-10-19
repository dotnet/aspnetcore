// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
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
        private readonly RequestDelegate _next;
        private readonly RequestLocalizationOptions _options;
        private readonly RequestCulture _defaultRequestCulture;

        /// <summary>
        /// Creates a new <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options">The <see cref="RequestLocalizationOptions"/> representing the options for the
        /// <see cref="RequestLocalizationMiddleware"/>.</param>
        /// <param name="defaultRequestCulture">The default <see cref="RequestCulture"/> to use if none of the
        /// requested cultures match supported cultures.</param>
        public RequestLocalizationMiddleware(
            RequestDelegate next,
            RequestLocalizationOptions options,
            RequestCulture defaultRequestCulture)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (defaultRequestCulture == null)
            {
                throw new ArgumentNullException(nameof(defaultRequestCulture));
            }

            _next = next;
            _options = options;
            _defaultRequestCulture = defaultRequestCulture;
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

            var requestCulture = _defaultRequestCulture;

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
                            cultureInfo = GetCultureInfo(cultures, _options.SupportedCultures);
                        }

                        if (_options.SupportedUICultures != null)
                        {
                            uiCultureInfo = GetCultureInfo(uiCultures, _options.SupportedUICultures);
                        }

                        if (cultureInfo == null && uiCultureInfo == null)
                        {
                            continue;
                        }
                        if (cultureInfo == null && uiCultureInfo != null)
                        {
                            cultureInfo = _defaultRequestCulture.Culture;
                        }
                        if (cultureInfo != null && uiCultureInfo == null)
                        {
                            uiCultureInfo = _defaultRequestCulture.UICulture;
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

        private CultureInfo GetCultureInfo(IList<string> cultures, IList<CultureInfo> supportedCultures)
        {
            foreach (var culture in cultures)
            {
                // Allow empty string values as they map to InvariantCulture, whereas null culture values will throw in
                // the CultureInfo ctor
                if (culture != null)
                {
                    var cultureInfo = CultureInfoCache.GetCultureInfo(culture, supportedCultures);
                    if (cultureInfo != null)
                    {
                        return cultureInfo;
                    }
                }
            }

            return null;
        }
    }
}