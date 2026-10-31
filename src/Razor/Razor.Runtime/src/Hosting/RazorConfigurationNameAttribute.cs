// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Hosting;

/// <summary>
/// Specifies the name of a Razor configuration as defined by the Razor SDK.
/// </summary>
/// <remarks>
/// This attribute is applied to an application's entry point assembly by the Razor SDK during the build,
/// so that the Razor configuration can be loaded at runtime based on the settings provided by the project
/// file.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class RazorConfigurationNameAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance of <see cref="RazorConfigurationNameAttribute"/>.
    /// </summary>
    /// <param name="configurationName">The name of the Razor configuration.</param>
    public RazorConfigurationNameAttribute(string configurationName)
    {
        ArgumentNullException.ThrowIfNull(configurationName);

        ConfigurationName = configurationName;
    }

    /// <summary>
    /// Gets the name of the Razor configuration.
    /// </summary>
    public string ConfigurationName { get; }
}
