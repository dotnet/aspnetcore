// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.MiddlewareAnalysis;

/// <summary>
/// An <see cref="IStartupFilter"/> that configures the middleware pipeline to log to a <see cref="System.Diagnostics.DiagnosticSource"/>
/// when middleware starts, finishes and throws.
/// </summary>
public class AnalysisStartupFilter : IStartupFilter
{
    /// <summary>
    /// Wraps the <see cref="IApplicationBuilder"/> with <see cref="AnalysisBuilder"/> and directly adds
    /// <see cref="AnalysisMiddleware"/> to the end of the middleware pipeline.
    /// </summary>
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            var wrappedBuilder = new AnalysisBuilder(builder);
            next(wrappedBuilder);

            // The caller doesn't call build on our new builder, they call it on the original. Add this
            // default middleware to the end. Compare with AnalysisBuilder.Build();

            // Add one maker at the end before the default 404 middleware (or any fancy Join middleware).
            builder.UseMiddleware<AnalysisMiddleware>("EndOfPipeline");
        };
    }
}
