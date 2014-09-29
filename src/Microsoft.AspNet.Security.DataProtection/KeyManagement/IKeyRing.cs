// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNet.Security.DataProtection.KeyManagement
{
    internal interface IKeyRing
    {
        IAuthenticatedEncryptor DefaultAuthenticatedEncryptor { get; }

        Guid DefaultKeyId { get; }

        IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(Guid keyId, out bool isRevoked);
    }
}
