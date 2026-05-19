// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class ConfigSectionClone
{
    public ConfigSectionClone(IConfigurationSection configSection)
    {
        Value = configSection.Value;

        // GetChildren() should return an empty IEnumerable instead of null, but we guard against it since it's a public interface.
        var children = configSection.GetChildren() ?? Enumerable.Empty<IConfigurationSection>();
        Children = children.ToDictionary(child => child.Key, child => new ConfigSectionClone(child));
    }

    public string? Value { get; }

    public Dictionary<string, ConfigSectionClone> Children { get; }

    public override bool Equals(object? obj)
    {
        if (!(obj is ConfigSectionClone other))
        {
            return false;
        }

        if (Value != other.Value || Children.Count != other.Children.Count)
        {
            return false;
        }

        foreach (var kvp in Children)
        {
            if (!other.Children.TryGetValue(kvp.Key, out var child))
            {
                return false;
            }

            if (kvp.Value != child)
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode() => HashCode.Combine(Value, Children.Count);

    public static bool operator ==(ConfigSectionClone lhs, ConfigSectionClone rhs) => lhs is null ? rhs is null : lhs.Equals(rhs);
    public static bool operator !=(ConfigSectionClone lhs, ConfigSectionClone rhs) => !(lhs == rhs);
}
