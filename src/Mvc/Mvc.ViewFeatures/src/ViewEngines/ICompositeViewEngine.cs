// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ViewEngines;

/// <summary>
/// Represents an <see cref="IViewEngine"/> that delegates to one of a collection of view engines.
/// </summary>
public interface ICompositeViewEngine : IViewEngine
{
    /// <summary>
    /// Gets the list of <see cref="IViewEngine"/> this instance of <see cref="ICompositeViewEngine"/> delegates
    /// to.
    /// </summary>
    IReadOnlyList<IViewEngine> ViewEngines { get; }
}
