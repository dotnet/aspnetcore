// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Enables automatic setting of the culture for <see cref="Http.HttpRequest"/>s based on information
    /// sent by the client in headers and logic provided by the application.
    /// </summary>
    public class RequestLocalizationMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates a new <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        public RequestLocalizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
        public async Task Invoke(HttpContext context)
        {
            // TODO: Make this read from Accept-Language header, cookie, app-provided delegate, etc.
            if (context.Request.QueryString.HasValue)
            {
                var queryCulture = context.Request.Query["culture"];
                if (!string.IsNullOrEmpty(queryCulture))
                {
                    var requestCulture = new RequestCulture(new CultureInfo(queryCulture));

                    context.SetFeature<IRequestCultureFeature>(new RequestCultureFeature(requestCulture));

                    var originalCulture = CultureInfo.CurrentCulture;
                    var originalUICulture = CultureInfo.CurrentUICulture;

                    SetCurrentCulture(requestCulture);

                    await _next(context);

                    return;
                }
            }
            else
            {
                // NOTE: The below doesn't seem to be needed anymore now that DNX is correctly managing culture across
                //       async calls but we'll need to verify properly.
                // Forcibly set thread to en-US as sometimes previous threads have wrong culture across async calls, 
                // see note above.
                //var defaultRequestCulture = new RequestCulture(new CultureInfo("en-US"));
                //SetCurrentCulture(defaultRequestCulture);
            }

            await _next(context);
        }

        private void SetCurrentCulture(RequestCulture requestCulture)
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