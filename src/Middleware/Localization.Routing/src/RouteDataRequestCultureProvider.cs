// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Localization.Routing
{
    /// <summary>
    /// Determines the culture information for a request via values in the route data.
    /// </summary>
    public class RouteDataRequestCultureProvider : RequestCultureProvider
    {
        /// <summary>
        /// The key that contains the culture name.
        /// Defaults to "culture".
        /// </summary>
        public string RouteDataStringKey { get; set; } = "culture";

        /// <summary>
        /// The key that contains the UI culture name. If not specified or no value is found,
        /// <see cref="RouteDataStringKey"/> will be used.
        /// Defaults to "ui-culture".
        /// </summary>
        public string UIRouteDataStringKey { get; set; } = "ui-culture";

        /// <inheritdoc />
        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            string culture = null;
            string uiCulture = null;

            if (!string.IsNullOrEmpty(RouteDataStringKey))
            {
                culture = httpContext.GetRouteValue(RouteDataStringKey)?.ToString();
            }

            if (!string.IsNullOrEmpty(UIRouteDataStringKey))
            {
                uiCulture = httpContext.GetRouteValue(UIRouteDataStringKey)?.ToString();
            }

            if (culture == null && uiCulture == null)
            {
                // No values specified for either so no match
                return NullProviderCultureResult;
            }

            if (culture != null && uiCulture == null)
            {
                // Value for culture but not for UI culture so default to culture value for both
                uiCulture = culture;
            }
            else if (culture == null && uiCulture != null)
            {
                // Value for UI culture but not for culture so default to UI culture value for both
                culture = uiCulture;
            }

            var providerResultCulture = new ProviderCultureResult(culture, uiCulture);

            return Task.FromResult(providerResultCulture);
        }
    }
}
