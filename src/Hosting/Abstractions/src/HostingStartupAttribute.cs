// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Marker attribute indicating an implementation of <see cref="IHostingStartup"/> that will be loaded and executed when building an <see cref="IWebHost"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class HostingStartupAttribute : Attribute
{
    /// <summary>
    /// Constructs the <see cref="HostingStartupAttribute"/> with the specified type.
    /// </summary>
    /// <param name="hostingStartupType">A type that implements <see cref="IHostingStartup"/>.</param>
    public HostingStartupAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type hostingStartupType)
    {
        ArgumentNullException.ThrowIfNull(hostingStartupType);

        if (!typeof(IHostingStartup).IsAssignableFrom(hostingStartupType))
        {
            throw new ArgumentException($@"""{hostingStartupType}"" does not implement {typeof(IHostingStartup)}.", nameof(hostingStartupType));
        }

        HostingStartupType = hostingStartupType;
    }

    /// <summary>
    /// The implementation of <see cref="IHostingStartup"/> that should be loaded when
    /// starting an application.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type HostingStartupType { get; }
}
