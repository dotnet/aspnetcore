// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    // Copied from https://github.com/dotnet/cli/blob/rel/1.0.0/src/Microsoft.DotNet.ProjectModel.Workspaces/SnkUtils.cs
    internal static class SnkUtils
    {
        const byte PUBLICKEYBLOB = 0x06;
        const byte PRIVATEKEYBLOB = 0x07;

        private const uint CALG_RSA_SIGN = 0x00002400;
        private const uint CALG_SHA = 0x00008004;

        private const uint RSA1 = 0x31415352;  //"RSA1" publickeyblob
        private const uint RSA2 = 0x32415352;  //"RSA2" privatekeyblob

        private const int VersionOffset = 1;
        private const int ModulusLengthOffset = 12;
        private const int ExponentOffset = 16;
        private const int MagicPrivateKeyOffset = 8;
        private const int MagicPublicKeyOffset = 20;

        public static ImmutableArray<byte> ExtractPublicKey(byte[] snk)
        {
            ValidateBlob(snk);

            if (snk[0] != PRIVATEKEYBLOB)
            {
                return ImmutableArray.Create(snk);
            }

            var version = snk[VersionOffset];
            int modulusBitLength = ReadInt32(snk, ModulusLengthOffset);
            uint exponent = (uint)ReadInt32(snk, ExponentOffset);
            var modulus = new byte[modulusBitLength >> 3];

            Array.Copy(snk, 20, modulus, 0, modulus.Length);

            return CreatePublicKey(version, exponent, modulus);
        }

        private static void ValidateBlob(byte[] snk)
        {
            // 160 - the size of public key
            if (snk.Length >= 160)
            {
                if (snk[0] == PRIVATEKEYBLOB && ReadInt32(snk, MagicPrivateKeyOffset) == RSA2 || // valid private key
                    snk[12] == PUBLICKEYBLOB && ReadInt32(snk, MagicPublicKeyOffset) == RSA1)  // valid public key
                {
                    return;
                }
            }

            throw new InvalidOperationException("Invalid key file.");
        }

        private static int ReadInt32(byte[] array, int index)
        {
            return array[index] | array[index + 1] << 8 | array[index + 2] << 16 | array[index + 3] << 24;
        }

        private static ImmutableArray<byte> CreatePublicKey(byte version, uint exponent, byte[] modulus)
        {
            using (var ms = new MemoryStream(160))
            using (var binaryWriter = new BinaryWriter(ms))
            {
                binaryWriter.Write(CALG_RSA_SIGN);
                binaryWriter.Write(CALG_SHA);
                // total size of the rest of the blob (20 - size of RSAPUBKEY)
                binaryWriter.Write(modulus.Length + 20);
                binaryWriter.Write(PUBLICKEYBLOB);
                binaryWriter.Write(version);
                binaryWriter.Write((ushort)0x00000000); // reserved
                binaryWriter.Write(CALG_RSA_SIGN);
                binaryWriter.Write(RSA1);
                binaryWriter.Write(modulus.Length << 3);
                binaryWriter.Write(exponent);
                binaryWriter.Write(modulus);
                return ImmutableArray.Create(ms.ToArray());
            }
        }
    }
}