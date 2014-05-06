// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Builder.Extensions;

namespace Microsoft.AspNet.Builder
{
    using Predicate = Func<HttpContext, bool>;
    using PredicateAsync = Func<HttpContext, Task<bool>>;

    /// <summary>
    /// Extension methods for the MapWhenMiddleware
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
        public static IBuilder MapWhen([NotNull] this IBuilder app, [NotNull] Predicate predicate, [NotNull] Action<IBuilder> configuration)
        {
            // create branch
            IBuilder branchBuilder = app.New();
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

        /// <summary>
        /// Branches the request pipeline based on the async result of the given predicate.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="predicate">Invoked asynchronously with the request environment to determine if the branch should be taken</param>
        /// <param name="configuration">Configures a branch to take</param>
        /// <returns></returns>
        public static IBuilder MapWhenAsync([NotNull] this IBuilder app, [NotNull] PredicateAsync predicate, [NotNull] Action<IBuilder> configuration)
        {
            // create branch
            IBuilder branchBuilder = app.New();
            configuration(branchBuilder);
            var branch = branchBuilder.Build();

            // put middleware in pipeline
            var options = new MapWhenOptions
            {
                PredicateAsync = predicate,
                Branch = branch,
            };
            return app.Use(next => new MapWhenMiddleware(next, options).Invoke);
        }
    }
}