// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.Managed;

internal static class SymmetricAlgorithmExtensions
{
    public static int GetBlockSizeInBytes(this SymmetricAlgorithm symmetricAlgorithm)
    {
        var blockSizeInBits = symmetricAlgorithm.BlockSize;
        CryptoUtil.Assert(blockSizeInBits >= 0 && blockSizeInBits % 8 == 0, "blockSizeInBits >= 0 && blockSizeInBits % 8 == 0");
        return blockSizeInBits / 8;
    }
}
