// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Determines the culture information for a request via the configured delegate.
    /// </summary>
    public class CustomRequestCultureProvider : RequestCultureProvider
    {
        private readonly Func<HttpContext, Task<RequestCulture>> _provider;

        /// <summary>
        /// Creates a new <see cref="CustomRequestCultureProvider"/> using the specified delegate.
        /// </summary>
        /// <param name="provider">The provider delegate.</param>
        public CustomRequestCultureProvider(Func<HttpContext, Task<RequestCulture>> provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _provider = provider;
        }

        /// <inheritdoc />
        public override Task<RequestCulture> DetermineRequestCulture(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            return _provider(httpContext);
        }
    }
}
