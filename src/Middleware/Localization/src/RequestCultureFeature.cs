// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;

namespace Microsoft.AspNetCore.Localization
{
    /// <summary>
    /// Provides the current request's culture information.
    /// </summary>
    public class RequestCultureFeature : IRequestCultureFeature
    {
        /// <summary>
        /// Creates a new <see cref="RequestCultureFeature"/> with the specified <see cref="Localization.RequestCulture"/>.
        /// </summary>
        /// <param name="requestCulture">The <see cref="Localization.RequestCulture"/>.</param>
        /// <param name="provider">The <see cref="IRequestCultureProvider"/>.</param>
        public RequestCultureFeature(RequestCulture requestCulture, IRequestCultureProvider provider)
        {
            if (requestCulture == null)
            {
                throw new ArgumentNullException(nameof(requestCulture));
            }

            RequestCulture = requestCulture;
            Provider = provider;
        }

        /// <inheritdoc />
        public RequestCulture RequestCulture { get; }

        /// <inheritdoc />
        public IRequestCultureProvider Provider { get; }
    }
}
