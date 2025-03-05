// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// An interface for EndpointHtmlRenderer implementations that allows NavigationManagers call renderer's methods
/// </summary>
public interface IEndpointHtmlRenderer
{
    /// <summary>
    /// Sets the html response to 404 Not Found.
    /// </summary>
    void SetNotFoundResponse();
}