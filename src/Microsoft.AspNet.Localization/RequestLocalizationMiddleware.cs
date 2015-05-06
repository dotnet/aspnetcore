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
        private readonly RequestLocalizationMiddlewareOptions _options;
        
        /// <summary>
        /// Creates a new <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options"></param>
        public RequestLocalizationMiddleware([NotNull] RequestDelegate next, [NotNull] RequestLocalizationMiddlewareOptions options)
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
                new RequestCulture(CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture);

            IRequestCultureStrategy winningStrategy = null;

            if (_options.RequestCultureStrategies != null)
            {
                foreach (var strategy in _options.RequestCultureStrategies)
                {
                    var result = strategy.DetermineRequestCulture(context);
                    if (result != null)
                    {
                        requestCulture = result;
                        winningStrategy = strategy;
                        break;
                    }
                }
            }

            context.SetFeature<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, winningStrategy));

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