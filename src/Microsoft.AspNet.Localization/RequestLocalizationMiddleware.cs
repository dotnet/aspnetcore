// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

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
        
        /// <summary>
        /// Creates a new <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options"></param>
        public RequestLocalizationMiddleware([NotNull] RequestDelegate next, [NotNull] RequestLocalizationOptions options)
        {
            _next = next;
            _options = options;
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
        public async Task Invoke([NotNull] HttpContext context)
        {
            var requestCulture = _options.DefaultRequestCulture ??
                new RequestCulture(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

            IRequestCultureProvider winningProvider = null;

            if (_options.RequestCultureProviders != null)
            {
                foreach (var provider in _options.RequestCultureProviders)
                {
                    var result = await provider.DetermineRequestCulture(context);
                    if (result != null)
                    {
                        requestCulture = result;
                        winningProvider = provider;
                        break;
                    }
                }
            }

            context.SetFeature<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, winningProvider));

            SetCurrentThreadCulture(requestCulture);

            await _next(context);
        }

        private static void SetCurrentThreadCulture(RequestCulture requestCulture)
        {
#if DNX451
            Thread.CurrentThread.CurrentCulture = requestCulture.Culture;
            Thread.CurrentThread.CurrentUICulture = requestCulture.UICulture;
#else
            CultureInfo.CurrentCulture = requestCulture.Culture;
            CultureInfo.CurrentUICulture = requestCulture.UICulture;
#endif
        }
    }
}