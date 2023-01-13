// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Executes a middleware pipeline provided the by the <see cref="MiddlewareFilterAttribute.ConfigurationType"/>.
/// The middleware pipeline will be treated as an async resource filter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class MiddlewareFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
{
    /// <summary>
    /// Instantiates a new instance of <see cref="MiddlewareFilterAttribute"/>.
    /// </summary>
    /// <param name="configurationType">A type which configures a middleware pipeline.</param>
    public MiddlewareFilterAttribute(Type configurationType)
    {
        ArgumentNullException.ThrowIfNull(configurationType);

        ConfigurationType = configurationType;
    }

    /// <summary>
    /// The type which configures a middleware pipeline.
    /// </summary>
    public Type ConfigurationType { get; }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public bool IsReusable => true;

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var middlewarePipelineService = serviceProvider.GetRequiredService<MiddlewareFilterBuilder>();
        var pipeline = middlewarePipelineService.GetPipeline(ConfigurationType);

        return new MiddlewareFilter(pipeline);
    }
}
