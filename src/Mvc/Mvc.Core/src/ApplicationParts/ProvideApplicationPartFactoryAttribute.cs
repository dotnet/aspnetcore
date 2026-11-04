// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// Provides a <see cref="ApplicationPartFactory"/> type.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ProvideApplicationPartFactoryAttribute : Attribute
{
    private readonly Type? _applicationPartFactoryType;
    private readonly string? _applicationPartFactoryTypeName;

    /// <summary>
    /// Creates a new instance of <see cref="ProvideApplicationPartFactoryAttribute"/> with the specified type.
    /// </summary>
    /// <param name="factoryType">The factory type.</param>
    public ProvideApplicationPartFactoryAttribute(Type factoryType)
    {
        _applicationPartFactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
    }

    /// <summary>
    /// Creates a new instance of <see cref="ProvideApplicationPartFactoryAttribute"/> with the specified type name.
    /// </summary>
    /// <param name="factoryTypeName">The assembly qualified type name.</param>
    public ProvideApplicationPartFactoryAttribute(string factoryTypeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(factoryTypeName);

        _applicationPartFactoryTypeName = factoryTypeName;
    }

    /// <summary>
    /// Gets the factory type.
    /// </summary>
    /// <returns></returns>
    public Type GetFactoryType()
    {
        return _applicationPartFactoryType ??
            Type.GetType(_applicationPartFactoryTypeName!, throwOnError: true)!;
    }
}
