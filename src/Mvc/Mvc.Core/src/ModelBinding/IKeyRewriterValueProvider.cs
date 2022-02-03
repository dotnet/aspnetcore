// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A value provider which can filter its contents to remove keys rewritten compared to the request data.
/// </summary>
public interface IKeyRewriterValueProvider : IValueProvider
{
    /// <summary>
    /// Filters the value provider to remove keys rewritten compared to the request data.
    /// </summary>
    /// <example>
    /// If the request contains values with keys <c>Model.Property</c> and <c>Collection[index]</c>, the returned
    /// <see cref="IValueProvider"/> will not match <c>Model[Property]</c> or <c>Collection.index</c>.
    /// </example>
    /// <returns>
    /// The filtered value provider or <see langref="null"/> if the value provider only contains rewritten keys.
    /// </returns>
    IValueProvider? Filter();
}
