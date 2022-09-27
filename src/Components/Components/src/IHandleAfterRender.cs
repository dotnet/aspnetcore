// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Interface implemented by components that receive notification that they have been rendered.
/// </summary>
public interface IHandleAfterRender
{
    /// <summary>
    /// Notifies the component that it has been rendered.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous event handling operation.</returns>
    Task OnAfterRenderAsync();
}
