// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.MiddlewareAnalysis;

/// <summary>
/// An <see cref="IApplicationBuilder"/> decorator used by <see cref="AnalysisStartupFilter"/>
/// to add <see cref="AnalysisMiddleware"/> before and after each other middleware in the pipeline.
/// </summary>
public class AnalysisBuilder : IApplicationBuilder
{
    private const string NextMiddlewareName = "analysis.NextMiddlewareName";

    /// <summary>
    /// Initializes a new instance of <see cref="AnalysisBuilder"/>.
    /// </summary>
    /// <param name="inner">The <see cref="IApplicationBuilder"/> to decorate.</param>
    public AnalysisBuilder(IApplicationBuilder inner)
    {
        InnerBuilder = inner;
    }

    private IApplicationBuilder InnerBuilder { get; }

    /// <inheritdoc />
    public IServiceProvider ApplicationServices
    {
        get { return InnerBuilder.ApplicationServices; }
        set { InnerBuilder.ApplicationServices = value; }
    }

    /// <inheritdoc />
    public IDictionary<string, object?> Properties
    {
        get { return InnerBuilder.Properties; }
    }

    /// <inheritdoc />
    public IFeatureCollection ServerFeatures
    {
        get { return InnerBuilder.ServerFeatures; }
    }

    /// <inheritdoc />
    public RequestDelegate Build()
    {
        // Add one maker at the end before the default 404 middleware (or any fancy Join middleware).
        return InnerBuilder.UseMiddleware<AnalysisMiddleware>("EndOfPipeline")
            .Build();
    }

    /// <inheritdoc />
    public IApplicationBuilder New()
    {
        return new AnalysisBuilder(InnerBuilder.New());
    }

    /// <inheritdoc />
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        var middlewareName = string.Empty; // UseMiddleware doesn't work with null params.
        if (Properties.TryGetValue(NextMiddlewareName, out var middlewareNameObj) && middlewareNameObj != null)
        {
            middlewareName = middlewareNameObj.ToString();
            Properties.Remove(NextMiddlewareName);
        }

        return InnerBuilder.UseMiddleware<AnalysisMiddleware>(middlewareName)
            .Use(middleware);
    }
}
