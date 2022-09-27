// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Interface implemented by components that receive notification of state changes.
/// </summary>
public interface IHandleEvent
{
    /// <summary>
    /// Notifies the a state change has been triggered.
    /// </summary>
    /// <param name="item">The <see cref="EventCallbackWorkItem"/> associated with this event.</param>
    /// <param name="arg">The argument associated with this event.</param>
    /// <returns>
    /// A <see cref="Task"/> that completes once the component has processed the state change.
    /// </returns>
    Task HandleEventAsync(EventCallbackWorkItem item, object? arg);
}
