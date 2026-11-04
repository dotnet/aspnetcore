// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Components.Testing.Generators;

internal sealed class ServiceOverrideCallsite : IEquatable<ServiceOverrideCallsite>
{
    public string TypeFullyQualifiedName { get; }

    public string TypeFullName { get; }

    public string AssemblyName { get; }

    public string MethodName { get; }

    public ServiceOverrideCallsite(
        string typeFullyQualifiedName,
        string typeFullName,
        string assemblyName,
        string methodName)
    {
        TypeFullyQualifiedName = typeFullyQualifiedName;
        TypeFullName = typeFullName;
        AssemblyName = assemblyName;
        MethodName = methodName;
    }

    public bool Equals(ServiceOverrideCallsite? other)
    {
        if (other is null)
        {
            return false;
        }

        return TypeFullyQualifiedName == other.TypeFullyQualifiedName &&
               MethodName == other.MethodName;
    }

    public override bool Equals(object? obj) => Equals(obj as ServiceOverrideCallsite);

    public override int GetHashCode()
    {
        unchecked
        {
            return (TypeFullyQualifiedName.GetHashCode() * 397) ^ MethodName.GetHashCode();
        }
    }
}
