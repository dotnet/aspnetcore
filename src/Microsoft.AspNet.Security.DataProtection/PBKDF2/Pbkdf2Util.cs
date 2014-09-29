// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.Cng;

namespace Microsoft.AspNet.Security.DataProtection.PBKDF2
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
            if (OSVersionUtil.IsBCryptOnWin8OrLaterAvailable())
            {
                // fastest implementation
                return new Win8Pbkdf2Provider();
            } else if (OSVersionUtil.IsBCryptOnWin7OrLaterAvailable())
            {
                // acceptable implementation
                return new Win7Pbkdf2Provider();
            } else
            {
                // slowest implementation
                return new ManagedPbkdf2Provider();
            }
        }
    }
}
