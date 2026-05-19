// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// <para>
/// Contains constant values for known filter scopes.
/// </para>
/// <para>
/// Scope defines the ordering of filters that have the same order. Scope is by-default
/// defined by how a filter is registered.
/// </para>
/// </summary>
public static class FilterScope
{
    /// <summary>
    /// First filter scope.
    /// </summary>
    public static readonly int First;

    /// <summary>
    /// Global filter scope.
    /// </summary>
    public static readonly int Global = 10;

    /// <summary>
    /// Controller filter scope.
    /// </summary>
    public static readonly int Controller = 20;

    /// <summary>
    /// Action filter scope.
    /// </summary>
    public static readonly int Action = 30;

    /// <summary>
    /// Last filter scope.
    /// </summary>
    public static readonly int Last = 100;
}
