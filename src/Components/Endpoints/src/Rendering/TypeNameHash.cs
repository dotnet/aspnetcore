// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints;

// Internal for testing.
internal class TypeNameHash
{
    public const int MaxStackBufferSize = 1024;

    public static string Compute(Type type)
    {
        if (type.FullName is not { } typeName)
        {
            throw new InvalidOperationException($"Cannot compute a hash for a type without a {nameof(Type.FullName)}.");
        }

        Span<byte> typeNameBytes = stackalloc byte[MaxStackBufferSize];

        if (!Encoding.UTF8.TryGetBytes(typeName, typeNameBytes, out var written))
        {
            typeNameBytes = Encoding.UTF8.GetBytes(typeName);
            written = typeNameBytes.Length;
        }

        Span<byte> typeNameHashBytes = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(typeNameBytes[..written], typeNameHashBytes);

        return Convert.ToHexString(typeNameHashBytes);
    }
}
