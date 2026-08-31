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

        // Try to encode into a stack buffer first to avoid allocations.
        Span<byte> typeNameBytes = stackalloc byte[MaxStackBufferSize];
        int written;
        byte[]? rented = null;
        try
        {
            if (!Encoding.UTF8.TryGetBytes(typeName, typeNameBytes, out written))
            {
                // Larger than the stack buffer - rent an array from the pool.
                var byteCount = Encoding.UTF8.GetByteCount(typeName);
                rented = ArrayPool<byte>.Shared.Rent(byteCount);
                written = Encoding.UTF8.GetBytes(typeName, rented);
                typeNameBytes = rented;
            }

            Span<byte> typeNameHashBytes = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(typeNameBytes[..written], typeNameHashBytes);
            return Convert.ToHexString(typeNameHashBytes);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
