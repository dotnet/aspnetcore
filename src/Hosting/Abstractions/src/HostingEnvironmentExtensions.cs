// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS0618 // Type or member is obsolete
namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostingEnvironment"/>.
/// </summary>
public static class HostingEnvironmentExtensions
{
    /// <summary>
    /// Checks if the current hosting environment name is <see cref="EnvironmentName.Development"/>.
    /// </summary>
    /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/>.</param>
    /// <returns>True if the environment name is <see cref="EnvironmentName.Development"/>, otherwise false.</returns>
    public static bool IsDevelopment(this IHostingEnvironment hostingEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        return hostingEnvironment.IsEnvironment(EnvironmentName.Development);
    }

    /// <summary>
    /// Checks if the current hosting environment name is <see cref="EnvironmentName.Staging"/>.
    /// </summary>
    /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/>.</param>
    /// <returns>True if the environment name is <see cref="EnvironmentName.Staging"/>, otherwise false.</returns>
    public static bool IsStaging(this IHostingEnvironment hostingEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        return hostingEnvironment.IsEnvironment(EnvironmentName.Staging);
    }

    /// <summary>
    /// Checks if the current hosting environment name is <see cref="EnvironmentName.Production"/>.
    /// </summary>
    /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/>.</param>
    /// <returns>True if the environment name is <see cref="EnvironmentName.Production"/>, otherwise false.</returns>
    public static bool IsProduction(this IHostingEnvironment hostingEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        return hostingEnvironment.IsEnvironment(EnvironmentName.Production);
    }

    /// <summary>
    /// Compares the current hosting environment name against the specified value.
    /// </summary>
    /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/>.</param>
    /// <param name="environmentName">Environment name to validate against.</param>
    /// <returns>True if the specified name is the same as the current environment, otherwise false.</returns>
    public static bool IsEnvironment(
        this IHostingEnvironment hostingEnvironment,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(hostingEnvironment);

        return string.Equals(
            hostingEnvironment.EnvironmentName,
            environmentName,
            StringComparison.OrdinalIgnoreCase);
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
