// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.ConcurrencyLimiter;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConcurrencyMiddlewareServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns></returns>
        public static ConcurrencyLimiterBuilder AddConcurrencyLimiter(this IServiceCollection services)
        {
            return new ConcurrencyLimiterBuilder(services);
        }
    }
}
