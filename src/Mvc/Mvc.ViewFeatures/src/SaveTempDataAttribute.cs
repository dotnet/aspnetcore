// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A filter that saves the <see cref="ITempDataDictionary"/> for a request.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SaveTempDataAttribute : Attribute, IFilterFactory, IOrderedFilter
{
    /// <summary>
    /// Initialize a new instance of <see cref="SaveTempDataAttribute"/>.
    /// </summary>
    public SaveTempDataAttribute()
    {
        // Since SaveTempDataFilter registers for a response's OnStarting callback, we want this filter to run
        // as early as possible to get the opportunity to register the call back before any other result filter
        // starts writing to the response stream.
        Order = int.MinValue + 100;
    }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public bool IsReusable => true;

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<SaveTempDataFilter>();
    }
}
