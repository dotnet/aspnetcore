// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

/// <summary>
/// Static class that adds extension methods to <see cref="IWebAssemblyHostEnvironment"/>.
/// </summary>
public static class WebAssemblyHostEnvironmentExtensions
{
    /// <summary>
    /// Checks if the current hosting environment name is <c>Development</c>.
    /// </summary>
    /// <param name="hostingEnvironment">An instance of <see cref="IWebAssemblyHostEnvironment"/>.</param>
    /// <returns>True if the environment name is <c>Development</c>, otherwise false.</returns>
    public static bool IsDevelopment(this IWebAssemblyHostEnvironment hostingEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        return hostingEnvironment.IsEnvironment("Development");
    }

    /// <summary>
    /// Checks if the current hosting environment name is <c>Staging</c>.
    /// </summary>
    /// <param name="hostingEnvironment">An instance of <see cref="IWebAssemblyHostEnvironment"/>.</param>
    /// <returns>True if the environment name is <c>Staging</c>, otherwise false.</returns>
    public static bool IsStaging(this IWebAssemblyHostEnvironment hostingEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        return hostingEnvironment.IsEnvironment("Staging");
    }

    /// <summary>
    /// Checks if the current hosting environment name is <c>Production</c>.
    /// </summary>
    /// <param name="hostingEnvironment">An instance of <see cref="IWebAssemblyHostEnvironment"/>.</param>
    /// <returns>True if the environment name is <c>Production</c>, otherwise false.</returns>
    public static bool IsProduction(this IWebAssemblyHostEnvironment hostingEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        return hostingEnvironment.IsEnvironment("Production");
    }

    /// <summary>
    /// Compares the current hosting environment name against the specified value.
    /// </summary>
    /// <param name="hostingEnvironment">An instance of <see cref="IWebAssemblyHostEnvironment"/>.</param>
    /// <param name="environmentName">Environment name to validate against.</param>
    /// <returns>True if the specified name is the same as the current environment, otherwise false.</returns>
    public static bool IsEnvironment(
        this IWebAssemblyHostEnvironment hostingEnvironment,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        return string.Equals(
            hostingEnvironment.Environment,
            environmentName,
            StringComparison.OrdinalIgnoreCase);
    }
}
