// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// Provides the location of resources for an Assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public class ResourceLocationAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="ResourceLocationAttribute"/>.
    /// </summary>
    /// <param name="resourceLocation">The location of resources for this Assembly.</param>
    public ResourceLocationAttribute(string resourceLocation)
    {
        ArgumentThrowHelper.ThrowIfNullOrEmpty(resourceLocation);

        ResourceLocation = resourceLocation;
    }

    /// <summary>
    /// The location of resources for this Assembly.
    /// </summary>
    public string ResourceLocation { get; }
}
