// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;

namespace Microsoft.AspNetCore.DataProtection.AzureKeyVault
{
    internal class KeyVaultClientWrapper : IKeyVaultWrappingClient
    {
        private readonly KeyVaultClient _client;

        public KeyVaultClientWrapper(KeyVaultClient client)
        {
            _client = client;
        }

        public Task<KeyOperationResult> UnwrapKeyAsync(string keyIdentifier, string algorithm, byte[] cipherText)
        {
            return _client.UnwrapKeyAsync(keyIdentifier, algorithm, cipherText);
        }

        public Task<KeyOperationResult> WrapKeyAsync(string keyIdentifier, string algorithm, byte[] cipherText)
        {
            return _client.WrapKeyAsync(keyIdentifier, algorithm, cipherText);
        }
    }
}