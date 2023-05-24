// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;

#nullable enable

public static class X509Utilities
{
    public static bool HasCertificateType
    {
        get
        {
            var certificateType = GetType("System.Security.Cryptography", "System.Security.Cryptography.X509Certificates.X509Certificate");

            // We're checking for members, rather than just the presence of the type,
            // because Debugger Display types may reference it without actually
            // causing a meaningful binary size increase.
            return certificateType is not null && GetMembers(certificateType).Any();
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern",
        Justification = "Returning null when the type is unreferenced is desirable")]
    private static Type? GetType(string assemblyName, string typeName)
    {
        return Type.GetType($"{typeName}, {assemblyName}");
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Returning null when the type is unreferenced is desirable")]
    private static MemberInfo[] GetMembers(Type type)
    {
        return type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }
}