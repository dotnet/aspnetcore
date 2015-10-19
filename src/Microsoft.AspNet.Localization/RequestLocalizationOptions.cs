// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Specifies options for the <see cref="RequestLocalizationMiddleware"/>.
    /// </summary>
    public class RequestLocalizationOptions
    {
        /// <summary>
        /// Creates a new <see cref="RequestLocalizationOptions"/> with default values.
        /// </summary>
        public RequestLocalizationOptions()
        {
            RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new QueryStringRequestCultureProvider { Options = this },
                new CookieRequestCultureProvider { Options = this },
                new AcceptLanguageHeaderRequestCultureProvider { Options = this }
            };
        }

        /// <summary>
        /// The cultures supported by the application. If this value is non-<c>null</c>, the
        /// <see cref="RequestLocalizationMiddleware"/> will only set the current request culture to an entry in this
        /// list. A value of <c>null</c> means all cultures are supported.
        /// Defaults to <c>null</c>.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Improves usability")]
        public IList<CultureInfo> SupportedCultures { get; set; }

        /// <summary>
        /// The UI cultures supported by the application. If this value is non-<c>null</c>, the
        /// <see cref="RequestLocalizationMiddleware"/> will only set the current request culture to an entry in this
        /// list. A value of <c>null</c> means all cultures are supported.
        /// Defaults to <c>null</c>.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Improves usability")]
        public IList<CultureInfo> SupportedUICultures { get; set; }

        /// <summary>
        /// An ordered list of providers used to determine a request's culture information. The first provider that
        /// returns a non-<c>null</c> result for a given request will be used.
        /// Defaults to the following:
        /// <list type="number">
        ///     <item><description><see cref="QueryStringRequestCultureProvider"/></description></item>
        ///     <item><description><see cref="CookieRequestCultureProvider"/></description></item>
        ///     <item><description><see cref="AcceptLanguageHeaderRequestCultureProvider"/></description></item>
        /// </list>
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Improves usability")]
        public IList<IRequestCultureProvider> RequestCultureProviders { get; set; }
    }
}
