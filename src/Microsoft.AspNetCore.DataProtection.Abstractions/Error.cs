// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection.Abstractions;

namespace Microsoft.AspNetCore.DataProtection
{
    internal static class Error
    {
        public static CryptographicException CryptCommon_GenericError(Exception inner = null)
        {
            return new CryptographicException(Resources.CryptCommon_GenericError, inner);
        }

        public static CryptographicException CryptCommon_PayloadInvalid()
        {
            string message = Resources.CryptCommon_PayloadInvalid;
            return new CryptographicException(message);
        }
    }
}
