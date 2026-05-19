// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// An abstraction used when grouping enum values for <see cref="ModelMetadata.EnumGroupedDisplayNamesAndValues"/>.
/// </summary>
public readonly struct EnumGroupAndName
{
    private readonly Func<string> _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumGroupAndName"/> structure. This constructor should
    /// not be used in any site where localization is important.
    /// </summary>
    /// <param name="group">The group name.</param>
    /// <param name="name">The name.</param>
    public EnumGroupAndName(string group, string name)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(name);

        Group = group;
        _name = () => name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumGroupAndName"/> structure.
    /// </summary>
    /// <param name="group">The group name.</param>
    /// <param name="name">A <see cref="Func{String}"/> which will return the name.</param>
    public EnumGroupAndName(
        string group,
        Func<string> name)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(name);

        Group = group;
        _name = name;
    }

    /// <summary>
    /// Gets the Group name.
    /// </summary>
    public string Group { get; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name => _name();
}
