// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Controllers;

/// <summary>
/// Provides methods for creation and disposal of controllers.
/// </summary>
public interface IControllerFactory
{
    /// <summary>
    /// Creates a new controller for the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context"><see cref="ControllerContext"/> for the action to execute.</param>
    /// <returns>The controller.</returns>
    object CreateController(ControllerContext context);

    /// <summary>
    /// Releases a controller instance.
    /// </summary>
    /// <param name="context"><see cref="ControllerContext"/> for the executing action.</param>
    /// <param name="controller">The controller.</param>
    void ReleaseController(ControllerContext context, object controller);

    /// <summary>
    /// Releases a controller instance asynchronously.
    /// </summary>
    /// <param name="context"><see cref="ControllerContext"/> for the executing action.</param>
    /// <param name="controller">The controller.</param>
    ValueTask ReleaseControllerAsync(ControllerContext context, object controller)
    {
        ReleaseController(context, controller);
        return default;
    }
}
