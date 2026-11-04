// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A <see cref="IValueProviderFactory"/> that creates <see cref="IValueProvider"/> instances that
/// read values from the request query-string.
/// </summary>
public class QueryStringValueProviderFactory : IValueProviderFactory
{
    /// <inheritdoc />
    public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var query = context.ActionContext.HttpContext.Request.Query;
        if (query != null && query.Count > 0)
        {
            var valueProvider = new QueryStringValueProvider(
                BindingSource.Query,
                query,
                CultureInfo.InvariantCulture);

            context.ValueProviders.Add(valueProvider);
        }

        return Task.CompletedTask;
    }
}
