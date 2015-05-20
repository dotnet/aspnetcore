// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// An abstract base class provider for determining the culture information of an <see cref="HttpRequest"/>.
    /// </summary>
    public abstract class RequestCultureProvider : IRequestCultureProvider
    {
        /// <summary>
        /// The current options for the <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        public RequestLocalizationOptions Options { get; set; }

        /// <inheritdoc />
        public abstract Task<RequestCulture> DetermineRequestCulture(HttpContext httpContext);

        /// <summary>
        /// Determines if the given <see cref="RequestCulture"/> is valid according to the currently configured.
        /// <see cref="RequestLocalizationOptions"/>.
        /// </summary>
        /// <param name="requestCulture">The <see cref="RequestCulture"/> to validate.</param>
        /// <returns>
        /// The original <see cref="RequestCulture"/> if it was valid, otherwise a new <see cref="RequestCulture"/>
        /// with values for <see cref="RequestCulture.Culture"/> and <see cref="RequestCulture.UICulture"/> that are
        /// valid for the current configuration, or <c>null</c> if neither <see cref="RequestCulture.Culture"/> or
        /// <see cref="RequestCulture.UICulture"/> were valid.
        /// </returns>
        protected RequestCulture ValidateRequestCulture(RequestCulture requestCulture)
        {
            if (requestCulture == null || Options == null)
            {
                return requestCulture;
            }

            var result = requestCulture;

            if (Options.SupportedCultures != null && !Options.SupportedCultures.Contains(result.Culture))
            {
                result = new RequestCulture(Options.DefaultRequestCulture.Culture, result.UICulture);
            }

            if (Options.SupportedUICultures != null && !Options.SupportedUICultures.Contains(result.UICulture))
            {
                result = new RequestCulture(result.Culture, Options.DefaultRequestCulture.UICulture);
            }

            if (requestCulture.Culture != result.Culture && requestCulture.UICulture != result.UICulture)
            {
                // Both cultures were invalid, just return null
                return null;
            }

            return result;
        }
    }
}
