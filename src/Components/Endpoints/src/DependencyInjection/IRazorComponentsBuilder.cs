// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// 
/// </summary>
public interface IRazorComponentsBuilder
{
    /// <summary>
    /// 
    /// </summary>
    public IServiceCollection Services { get; }
}
