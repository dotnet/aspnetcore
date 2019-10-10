// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
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

            if (culture == null && uiCulture != null)
            {
                // Value for UI culture but not for culture so default to UI culture value for both
                culture = uiCulture;
            }

            culture = TwoLetterISoToFullName(culture, false);
            uiCulture = TwoLetterISoToFullName(uiCulture, true);

            var providerResultCulture = new ProviderCultureResult(culture, uiCulture);

            return Task.FromResult(providerResultCulture);
        }
        /// <summary>
        /// Find the first culture name from the "Supported Cultures" match the culture parameter if it passed as the two-letter ISO language name 
        /// EX: if culture = "en" then the function will find the first supported culture has two-letter ISO language name equals to "en" (case insensitive) may "en-US" or "en-GB" or etc. will be selected if it exists at first
        /// </summary>
        string TwoLetterISoToFullName(string culture, bool isUICulture)
        {
            System.Diagnostics.Debug.WriteLine($"{isUICulture} {this.Options?.SupportedUICultures?.Any()}");
            if (culture.Length == 2 && this.Options != null)
            {
                var cultures = isUICulture ? this.Options.SupportedUICultures : this.Options.SupportedCultures;
                culture = cultures?.Where(c => c.TwoLetterISOLanguageName.Equals(culture, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Name).FirstOrDefault() ?? culture;
            }
            return culture;
        }
    }
}
