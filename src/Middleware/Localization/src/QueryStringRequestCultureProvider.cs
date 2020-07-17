// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Localization
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
        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var request = httpContext.Request;
            if (!request.QueryString.HasValue)
            {
                return NullProviderCultureResult;
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
                return NullProviderCultureResult;
            }

            if (queryCulture != null && queryUICulture == null)
            {
                // Value for culture but not for UI culture so default to culture value for both
                queryUICulture = queryCulture;
            }
            else if (queryCulture == null && queryUICulture != null)
            {
                // Value for UI culture but not for culture so default to UI culture value for both
                queryCulture = queryUICulture;
            }

            var providerResultCulture = new ProviderCultureResult(queryCulture, queryUICulture);

            return Task.FromResult(providerResultCulture);
        }
    }
}
