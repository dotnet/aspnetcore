// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

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
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(configuration);

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
