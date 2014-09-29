// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection.Dpapi
{
    internal interface IProtectedData
    {
        byte[] Protect(byte[] userData, byte[] optionalEntropy, DataProtectionScope scope);

        byte[] Unprotect(byte[] encryptedData, byte[] optionalEntropy, DataProtectionScope scope);
    }
}
