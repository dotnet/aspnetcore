// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Provides a predicate which can determines which model properties or parameters should be bound by model binding.
/// </summary>
public interface IPropertyFilterProvider
{
    /// <summary>
    /// <para>
    /// Gets a predicate which can determines which model properties should be bound by model binding.
    /// </para>
    /// <para>
    /// This predicate is also used to determine which parameters are bound when a model's constructor is bound.
    /// </para>
    /// </summary>
    Func<ModelMetadata, bool> PropertyFilter { get; }
}
