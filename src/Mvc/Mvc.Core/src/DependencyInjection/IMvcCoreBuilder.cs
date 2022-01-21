// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// An interface for configuring essential MVC services.
/// </summary>
public interface IMvcCoreBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> where essential MVC services are configured.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the <see cref="ApplicationPartManager"/> where <see cref="ApplicationPart"/>s
    /// are configured.
    /// </summary>
    ApplicationPartManager PartManager { get; }
}
