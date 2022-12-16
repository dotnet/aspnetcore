// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

internal static class ManagedAlgorithmHelpers
{
    private static readonly List<Type> KnownAlgorithmTypes = new List<Type>
    {
        typeof(Aes),
        typeof(HMACSHA1),
        typeof(HMACSHA256),
        typeof(HMACSHA384),
        typeof(HMACSHA512)
    };

    // Any changes to this method should also be be reflected in FriendlyNameToType.
    public static string TypeToFriendlyName(Type type)
    {
        if (KnownAlgorithmTypes.Contains(type))
        {
            return type.Name;
        }
        else
        {
            return type.AssemblyQualifiedName!;
        }
    }

    // Any changes to this method should also be be reflected in TypeToFriendlyName.
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    [UnconditionalSuppressMessage("Trimmer", "IL2075", Justification = "Unknown type is checked for whether it has a public parameterless constructor. Handle trimmed types by providing a useful error message.")]
    [UnconditionalSuppressMessage("Trimmer", "IL2073", Justification = "Unknown type is checked for whether it has a public parameterless constructor. Handle trimmed types by providing a useful error message.")]
    public static Type FriendlyNameToType(string typeName)
    {
        foreach (var knownType in KnownAlgorithmTypes)
        {
            if (knownType.Name == typeName)
            {
                return knownType;
            }
        }

        var type = TypeExtensions.GetTypeWithTrimFriendlyErrorMessage(typeName);

        // Type name could be full or assembly qualified name of known type.
        if (KnownAlgorithmTypes.Contains(type))
        {
            return type;
        }

        // All other types are created using Activator.CreateInstance. Validate it has a valid constructor.
        if (type.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new InvalidOperationException($"Algorithm type {type} doesn't have a public parameterless constructor. If the app is published with trimming then the constructor may have been trimmed. Ensure the type's assembly is excluded from trimming.");
        }

        return type;
    }
}
