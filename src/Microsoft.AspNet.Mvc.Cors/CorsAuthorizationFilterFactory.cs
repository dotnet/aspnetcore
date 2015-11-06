// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Cors
{
    /// <summary>
    /// A filter factory which creates a new instance of <see cref="CorsAuthorizationFilter"/>.
    /// </summary>
    public class CorsAuthorizationFilterFactory : IFilterFactory, IOrderedFilter
    {
        private readonly string _policyName;

        /// <summary>
        /// Creates a new instance of <see cref="CorsAuthorizationFilterFactory"/>.
        /// </summary>
        /// <param name="policyName">Name used to fetch a CORS policy.</param>
        public CorsAuthorizationFilterFactory(string policyName)
        {
            _policyName = policyName;
        }

        /// <inheritdoc />
        public int Order
        {
            get
            {
                // Since clients' preflight requests would not have data to authenticate requests, this
                // filter must run before any other authorization filters.
                return int.MinValue + 100;
            }
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var filter = serviceProvider.GetRequiredService<CorsAuthorizationFilter>();
            filter.PolicyName = _policyName;
            return filter;
        }
    }
}
