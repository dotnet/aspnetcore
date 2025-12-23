// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// 
/// </summary>
public interface ITempDataProvider
{
    /// <summary>
    /// 
    /// </summary>
    IDictionary<string, object?> LoadTempData(HttpContext context);

    /// <summary>
    /// 
    /// </summary>
    void SaveTempData(HttpContext context, IDictionary<string, object?> values);
}
