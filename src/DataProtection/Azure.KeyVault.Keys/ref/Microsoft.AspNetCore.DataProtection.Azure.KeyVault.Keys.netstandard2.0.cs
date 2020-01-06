// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    public static partial class AzureDataProtectionBuilderExtensions
    {
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithAzureKeyVault(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Azure.Core.Cryptography.IKeyEncryptionKeyResolver client, string keyIdentifier) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithAzureKeyVault(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, string keyIdentifier) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder ProtectKeysWithAzureKeyVault(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, string keyIdentifier, Azure.Core.TokenCredential tokenCredential) { throw null; }
    }
}
