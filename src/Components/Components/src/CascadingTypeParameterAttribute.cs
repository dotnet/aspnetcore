// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Denotes the generic type parameter as cascading. This allows generic type inference
/// to use this type parameter value automatically on descendants that also have a type
/// parameter with the same name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CascadingTypeParameterAttribute : Attribute
{
    /// <summary>
    /// Constructs an instance of <see cref="CascadingTypeParameterAttribute"/>.
    /// </summary>
    /// <param name="name">The name of the type parameter.</param>
    public CascadingTypeParameterAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    /// <summary>
    /// Gets the name of the type parameter.
    /// </summary>
    public string Name { get; }
}
