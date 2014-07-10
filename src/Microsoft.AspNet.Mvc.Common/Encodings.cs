// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.AspNet.Mvc
{
    internal static class Encodings
    {
        /// <summary>
        /// Returns UTF8 Encoding without BOM and throws on invalid bytes
        /// </summary>
        public static readonly Encoding UTF8EncodingWithoutBOM
            = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    }
}