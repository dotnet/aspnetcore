// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Globalization;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Determines the culture information for a request via values in the query string.
    /// </summary>
    public class QueryStringRequestCultureProvider : RequestCultureProvider
    {
        /// <summary>
        /// The key that contains the culture name.
        /// Defaults to "culture".
        /// </summary>
        public string QueryStringKey { get; set; } = "culture";

        /// <summary>
        /// The key that contains the UI culture name. If not specified or no value is found,
        /// <see cref="QueryStringKey"/> will be used.
        /// Defaults to "ui-culture".
        /// </summary>
        public string UIQueryStringKey { get; set; } = "ui-culture";

        /// <inheritdoc />
        public override Task<RequestCulture> DetermineRequestCulture([NotNull] HttpContext httpContext)
        {
            var request = httpContext.Request;
            if (!request.QueryString.HasValue)
            {
                return Task.FromResult((RequestCulture)null);
            }

            string queryCulture = null;
            string queryUICulture = null;

            if (!string.IsNullOrWhiteSpace(QueryStringKey))
            {
                queryCulture = request.Query[QueryStringKey];
            }

            if (!string.IsNullOrWhiteSpace(UIQueryStringKey))
            {
                queryUICulture = request.Query[UIQueryStringKey];
            }

            if (queryCulture == null && queryUICulture == null)
            {
                // No values specified for either so no match
                return Task.FromResult((RequestCulture)null);
            }

            if (queryCulture != null && queryUICulture == null)
            {
                // Value for culture but not for UI culture so default to culture value for both
                queryUICulture = queryCulture;
            }

            var culture = CultureInfoCache.GetCultureInfo(queryCulture);
            var uiCulture = CultureInfoCache.GetCultureInfo(queryUICulture);

            if (culture == null || uiCulture == null)
            {
                return Task.FromResult((RequestCulture)null);
            }

            var requestCulture = new RequestCulture(culture, uiCulture);

            requestCulture = ValidateRequestCulture(requestCulture);

            return Task.FromResult(requestCulture);
        }
    }
}
