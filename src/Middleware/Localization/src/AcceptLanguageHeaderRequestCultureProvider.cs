// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Localization
{
    /// <summary>
    /// Determines the culture information for a request via the value of the Accept-Language header.
    /// </summary>
    public class AcceptLanguageHeaderRequestCultureProvider : RequestCultureProvider
    {
        /// <summary>
        /// The maximum number of values in the Accept-Language header to attempt to create a <see cref="System.Globalization.CultureInfo"/>
        /// from for the current request.
        /// Defaults to <c>3</c>.
        /// </summary>
        public int MaximumAcceptLanguageHeaderValuesToTry { get; set; } = 3;

        /// <inheritdoc />
        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var acceptLanguageHeader = httpContext.Request.GetTypedHeaders().AcceptLanguage;

            if (acceptLanguageHeader == null || acceptLanguageHeader.Count == 0)
            {
                return NullProviderCultureResult;
            }

            var languages = acceptLanguageHeader.AsEnumerable();

            if (MaximumAcceptLanguageHeaderValuesToTry > 0)
            {
                // We take only the first configured number of languages from the header and then order those that we
                // attempt to parse as a CultureInfo to mitigate potentially spinning CPU on lots of parse attempts.
                languages = languages.Take(MaximumAcceptLanguageHeaderValuesToTry);
            }

            var orderedLanguages = languages.OrderByDescending(h => h, StringWithQualityHeaderValueComparer.QualityComparer)
                .Select(x => x.Value).ToList();

            if (orderedLanguages.Count > 0)
            {
                return Task.FromResult(new ProviderCultureResult(orderedLanguages));
            }

            return NullProviderCultureResult;
        }
    }
}
