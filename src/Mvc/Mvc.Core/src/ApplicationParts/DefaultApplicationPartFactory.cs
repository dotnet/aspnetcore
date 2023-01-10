// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// Default <see cref="ApplicationPartFactory"/>.
/// </summary>
public class DefaultApplicationPartFactory : ApplicationPartFactory
{
    /// <summary>
    /// Gets an instance of <see cref="DefaultApplicationPartFactory"/>.
    /// </summary>
    public static DefaultApplicationPartFactory Instance { get; } = new DefaultApplicationPartFactory();

    /// <summary>
    /// Gets the sequence of <see cref="ApplicationPart"/> instances that are created by this instance of <see cref="DefaultApplicationPartFactory"/>.
    /// <para>
    /// Applications may use this method to get the same behavior as this factory produces during MVC's default part discovery.
    /// </para>
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/>.</param>
    /// <returns>The sequence of <see cref="ApplicationPart"/> instances.</returns>
    public static IEnumerable<ApplicationPart> GetDefaultApplicationParts(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        yield return new AssemblyPart(assembly);
    }

    /// <inheritdoc />
    public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
    {
        return GetDefaultApplicationParts(assembly);
    }
}
