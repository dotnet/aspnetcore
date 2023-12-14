// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A filter that will use the format value in the route data or query string to set the content type on an
/// <see cref="ObjectResult" /> returned from an action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class FormatFilterAttribute : Attribute, IFilterFactory
{
    /// <inheritdoc />
    public bool IsReusable => true;

    /// <summary>
    /// Creates an instance of <see cref="FormatFilter"/>.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <returns>An instance of <see cref="FormatFilter"/>.</returns>
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider.GetRequiredService<FormatFilter>();
    }
}
