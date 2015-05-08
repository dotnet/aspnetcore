// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Determines the culture information for a request via the configured delegate.
    /// </summary>
    public class CustomRequestCultureStrategy : IRequestCultureStrategy
    {
        private readonly Func<HttpContext, RequestCulture> _strategy;

        /// <summary>
        /// Creates a new <see cref="CustomRequestCultureStrategy"/> using the specified delegate.
        /// </summary>
        /// <param name="strategy">The strategy delegate.</param>
        public CustomRequestCultureStrategy([NotNull] Func<HttpContext, RequestCulture> strategy)
        {
            _strategy = strategy;
        }

        /// <inheritdoc />
        public RequestCulture DetermineRequestCulture([NotNull] HttpContext httpContext)
        {
            return _strategy(httpContext);
        }
    }
}
