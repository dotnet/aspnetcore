// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Used to intercept changes to the browser's location.
/// </summary>
public interface IHandleLocationChanging
{
    /// <summary>
    /// Invoked before the browser's location changes.
    /// </summary>
    /// <param name="context">The context for the navigation event.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    ValueTask OnLocationChanging(LocationChangingContext context);
}
