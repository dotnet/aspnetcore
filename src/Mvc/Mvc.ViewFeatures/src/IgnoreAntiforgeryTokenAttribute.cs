// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A filter that skips antiforgery token validation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class IgnoreAntiforgeryTokenAttribute : Attribute, IAntiforgeryPolicy, IOrderedFilter
{
    /// <summary>
    /// Gets the order value for determining the order of execution of filters. Filters execute in
    /// ascending numeric value of the <see cref="Order"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Filters are executed in an ordering determined by an ascending sort of the <see cref="Order"/> property.
    /// </para>
    /// <para>
    /// The default Order for this attribute is 1000 because it must run after any filter which does authentication
    /// or login in order to allow them to behave as expected (ie Unauthenticated or Redirect instead of 400).
    /// </para>
    /// <para>
    /// Look at <see cref="IOrderedFilter.Order"/> for more detailed info.
    /// </para>
    /// </remarks>
    public int Order { get; set; } = 1000;
}
