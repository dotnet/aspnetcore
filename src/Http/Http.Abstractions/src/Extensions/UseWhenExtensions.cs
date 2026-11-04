// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

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
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(configuration);

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
