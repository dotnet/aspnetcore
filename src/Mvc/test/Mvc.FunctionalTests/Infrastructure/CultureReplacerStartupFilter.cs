// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// Inserts the <see cref="CultureReplacerMiddleware"/> at the beginning of the pipeline.
/// </summary>
public class CultureReplacerStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return AddCulture;

        void AddCulture(IApplicationBuilder builder)
        {
            builder.UseMiddleware<CultureReplacerMiddleware>();
            next(builder);
        }
    }
}
