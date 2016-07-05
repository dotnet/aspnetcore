// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    using Predicate = Func<HttpContext, bool>;

    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class UseWhenExtensions
    {
        /// <summary>
        /// Conditionally creates a branch in the request pipeline that is rejoined to the main pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="predicate">Invoked with the request environment to determine if the branch should be taken</param>
        /// <param name="configuration">Configures a branch to take</param>
        /// <returns></returns>
        public static IApplicationBuilder UseWhen(this IApplicationBuilder app, Predicate predicate, Action<IApplicationBuilder> configuration)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Create and configure the branch builder right away; otherwise,
            // we would end up running our branch after all the components
            // that were subsequently added to the main builder.
            var branchBuilder = app.New();
            configuration(branchBuilder);

            return app.Use(main =>
            {
                // This is called only when the main application builder 
                // is built, not per request.
                branchBuilder.Run(main);
                var branch = branchBuilder.Build();

                return context =>
                {
                    if (predicate(context))
                    {
                        return branch(context);
                    }
                    else
                    {
                        return main(context);
                    }
                };
            });
        }
    }
}