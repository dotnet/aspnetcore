// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    internal interface IAuthenticatedEncryptor2 : IAuthenticatedEncryptor
    {
        byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize);
    }
}
