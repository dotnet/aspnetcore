// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder.Extensions;


namespace Microsoft.AspNetCore.Builder
{
    using Predicate = Func<HttpContext, bool>;

    /// <summary>
    /// Extension methods for the <see cref="MapWhenMiddleware"/>.
    /// </summary>
    public static class MapWhenExtensions
    {
        /// <summary>
        /// Branches the request pipeline based on the result of the given predicate.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="predicate">Invoked with the request environment to determine if the branch should be taken</param>
        /// <param name="configuration">Configures a branch to take</param>
        /// <returns></returns>
        public static IApplicationBuilder MapWhen(this IApplicationBuilder app, Predicate predicate, Action<IApplicationBuilder> configuration)
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

            // create branch
            var branchBuilder = app.New();
            configuration(branchBuilder);
            var branch = branchBuilder.Build();

            // put middleware in pipeline
            var options = new MapWhenOptions
            {
                Predicate = predicate,
                Branch = branch,
            };
            return app.Use(next => new MapWhenMiddleware(next, options).Invoke);
        }
    }
}