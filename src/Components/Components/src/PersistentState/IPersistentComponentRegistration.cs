// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal interface IPersistentComponentRegistration : IComparable<IPersistentComponentRegistration>, IEquatable<IPersistentComponentRegistration>
{
    public string Assembly { get; }
    public string FullTypeName { get; }

    public IComponentRenderMode? GetRenderModeOrDefault();

    int IComparable<IPersistentComponentRegistration>.CompareTo(IPersistentComponentRegistration? other)
    {
        var assemblyComparison = string.Compare(Assembly, other?.Assembly, StringComparison.Ordinal);
        if (assemblyComparison != 0)
        {
            return assemblyComparison;
        }
        return string.Compare(FullTypeName, other?.FullTypeName, StringComparison.Ordinal);
    }

    bool IEquatable<IPersistentComponentRegistration>.Equals(IPersistentComponentRegistration? other)
    {
        return string.Equals(Assembly, other?.Assembly, StringComparison.Ordinal) &&
            string.Equals(FullTypeName, other?.FullTypeName, StringComparison.Ordinal);
    }
}
