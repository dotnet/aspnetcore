using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Extensions;

namespace Microsoft.AspNet
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