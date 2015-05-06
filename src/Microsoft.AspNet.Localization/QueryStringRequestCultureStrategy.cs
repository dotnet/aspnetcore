// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Localization.Internal;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Determines the culture information for a request via values in the query string.
    /// </summary>
    public class QueryStringRequestCultureStrategy : IRequestCultureStrategy
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
        public RequestCulture DetermineRequestCulture([NotNull] HttpContext httpContext)
        {
            var request = httpContext.Request;
            if (!request.QueryString.HasValue)
            {
                return null;
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
                return null;
            }

            if (queryCulture != null && queryUICulture == null)
            {
                // Value for culture but not for UI culture so default to culture value for both
                queryUICulture = queryCulture;
            }

            return new RequestCulture(
                CultureUtilities.GetCultureFromName(queryCulture),
                CultureUtilities.GetCultureFromName(queryUICulture));
        }
    }
}
