// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder that can be used to configure Razor Components.
/// </summary>
public interface IRazorComponentsBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/>.
    /// </summary>
    public IServiceCollection Services { get; }
}
