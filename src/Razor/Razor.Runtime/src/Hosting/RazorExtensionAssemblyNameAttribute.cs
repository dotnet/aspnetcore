// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Hosting;

/// <summary>
/// Specifies the name of a Razor extension as defined by the Razor SDK.
/// </summary>
/// <remarks>
/// This attribute is applied to an application's entry point assembly by the Razor SDK during the build,
/// so that the Razor configuration can be loaded at runtime based on the settings provided by the project
/// file.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class RazorExtensionAssemblyNameAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance of <see cref="RazorExtensionAssemblyNameAttribute"/>.
    /// </summary>
    /// <param name="extensionName">The name of the extension.</param>
    /// <param name="assemblyName">The assembly name of the extension.</param>
    public RazorExtensionAssemblyNameAttribute(string extensionName, string assemblyName)
    {
        ArgumentNullException.ThrowIfNull(extensionName);
        ArgumentNullException.ThrowIfNull(assemblyName);

        ExtensionName = extensionName;
        AssemblyName = assemblyName;
    }

    /// <summary>
    /// Gets the assembly name of the extension.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the name of the extension.
    /// </summary>
    public string ExtensionName { get; }
}
