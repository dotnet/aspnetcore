// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    public static class RazorFileHash
    {
        /// <summary>
        /// Version 1 of the hash algorithm used for generating hashes of Razor files.
        /// </summary>
        public static readonly int HashAlgorithmVersion1 = 1;

        public static string GetHash([NotNull] IFileInfo file, int hashAlgorithmVersion)
        {
            if (hashAlgorithmVersion != HashAlgorithmVersion1)
            {
                throw new ArgumentException(Resources.RazorHash_UnsupportedHashAlgorithm,
                                            nameof(hashAlgorithmVersion));
            }

            try
            {
                using (var stream = file.CreateReadStream())
                {
                    return Crc32.Calculate(stream).ToString(CultureInfo.InvariantCulture);
                }
            }
            catch (IOException)
            {
                // Don't throw if reading the file fails.
            }

            return string.Empty;
        }
    }
}