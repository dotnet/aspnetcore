// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A filter factory which creates a new instance of <see cref="CorsAuthorizationFilter"/>.
    /// </summary>
    public class CorsAuthorizationFilterFactory : IFilterFactory, IOrderedFilter
    {
        private readonly string _policyName;

        /// <summary>
        /// Creates a new insntace of <see cref="CorsAuthorizationFilterFactory"/>.
        /// </summary>
        /// <param name="policyName"></param>
        public CorsAuthorizationFilterFactory(string policyName)
        {
            _policyName = policyName;
        }

        /// <inheritdoc />
        public int Order
        {
            get
            {
                return DefaultOrder.DefaultCorsSortOrder;
            }
        }

        public IFilter CreateInstance([NotNull] IServiceProvider serviceProvider)
        {
            var filter = serviceProvider.GetRequiredService<CorsAuthorizationFilter>();
            filter.PolicyName = _policyName;
            return filter;
        }
    }
}
