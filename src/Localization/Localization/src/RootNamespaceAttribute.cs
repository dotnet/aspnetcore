// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// Provides the RootNamespace of an Assembly. The RootNamespace of the assembly is used by Localization to
/// determine the resource name to look for when RootNamespace differs from the AssemblyName.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public class RootNamespaceAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="RootNamespaceAttribute"/>.
    /// </summary>
    /// <param name="rootNamespace">The RootNamespace for this Assembly.</param>
    public RootNamespaceAttribute(string rootNamespace)
    {
        ArgumentThrowHelper.ThrowIfNullOrEmpty(rootNamespace);

        RootNamespace = rootNamespace;
    }

    /// <summary>
    /// The RootNamespace of this Assembly. The RootNamespace of the assembly is used by Localization to
    /// determine the resource name to look for when RootNamespace differs from the AssemblyName.
    /// </summary>
    public string RootNamespace { get; }
}
