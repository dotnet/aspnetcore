// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNet.Security.DataProtection.Cng.PBKDF2
{
    /// <summary>
    /// Internal base class used for abstracting away the PBKDF2 implementation since the implementation is OS-specific.
    /// </summary>
    internal static class Pbkdf2Util
    {
        public static readonly IPbkdf2Provider Pbkdf2Provider = GetPbkdf2Provider();
        public static readonly UTF8Encoding SecureUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        private static IPbkdf2Provider GetPbkdf2Provider()
        {
            // In priority order, our three implementations are Win8, Win7, and "other".

            // TODO: Provide Win7 & Win8 implementations when the new DataProtection stack is fully copied over.
            return new ManagedPbkdf2Provider();
        }
    }
}
