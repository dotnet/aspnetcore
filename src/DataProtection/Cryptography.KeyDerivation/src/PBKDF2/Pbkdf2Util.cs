// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Cryptography.Cng;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2
{
    /// <summary>
    /// Internal base class used for abstracting away the PBKDF2 implementation since the implementation is OS-specific.
    /// </summary>
    internal static class Pbkdf2Util
    {
        public static readonly IPbkdf2Provider Pbkdf2Provider = GetPbkdf2Provider();

        private static IPbkdf2Provider GetPbkdf2Provider()
        {
            // In priority order, our three implementations are Win8, Win7, and "other".
            if (OSVersionUtil.IsWindows8OrLater())
            {
                // fastest implementation
                return new Win8Pbkdf2Provider();
            }
            else if (OSVersionUtil.IsWindows())
            {
                // acceptable implementation
                return new Win7Pbkdf2Provider();
            }
            else
            {
#if NETSTANDARD2_0
                return new ManagedPbkdf2Provider();
#elif NETCOREAPP
                // fastest implementation on .NET Core for Linux/macOS.
                // Not supported on .NET Framework
                return new NetCorePbkdf2Provider();
#else
#error Update target frameworks
#endif
            }
        }
    }
}
