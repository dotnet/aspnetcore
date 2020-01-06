// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Cryptography;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.AspNetCore.DataProtection.Azure.KeyVault;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Contains Azure KeyVault-specific extension methods for modifying a <see cref="IDataProtectionBuilder"/>.
    /// </summary>
    public static class AzureDataProtectionBuilderExtensions
    {
        /// <summary>
        /// Configures the data protection system to protect keys with specified key in Azure KeyVault.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="keyIdentifier">The Azure KeyVault key identifier used for key encryption.</param>
        /// <returns>The value <paramref name="builder"/>.</returns>
        public static IDataProtectionBuilder ProtectKeysWithAzureKeyVault(this IDataProtectionBuilder builder, string keyIdentifier)
        {
            return ProtectKeysWithAzureKeyVault(builder, keyIdentifier, new DefaultAzureCredential());
        }

        /// <summary>
        /// Configures the data protection system to protect keys with specified key in Azure KeyVault.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="keyIdentifier">The Azure KeyVault key identifier used for key encryption.</param>
        /// <param name="tokenCredential">The token credential to use for authentication.</param>
        /// <returns>The value <paramref name="builder"/>.</returns>
        public static IDataProtectionBuilder ProtectKeysWithAzureKeyVault(this IDataProtectionBuilder builder, string keyIdentifier, TokenCredential tokenCredential)
        {
            return ProtectKeysWithAzureKeyVault(builder, new KeyResolver(tokenCredential), keyIdentifier);
        }

        /// <summary>
        /// Configures the data protection system to protect keys with specified key in Azure KeyVault.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="client">The <see cref="IKeyEncryptionKeyResolver"/> to use for KeyVault access.</param>
        /// <param name="keyIdentifier">The Azure KeyVault key identifier used for key encryption.</param>
        /// <returns>The value <paramref name="builder"/>.</returns>
        public static IDataProtectionBuilder ProtectKeysWithAzureKeyVault(this IDataProtectionBuilder builder, IKeyEncryptionKeyResolver client, string keyIdentifier)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (string.IsNullOrEmpty(keyIdentifier))
            {
                throw new ArgumentException(nameof(keyIdentifier));
            }

            builder.Services.AddSingleton<IKeyEncryptionKeyResolver>(client);
            builder.Services.Configure<KeyManagementOptions>(options =>
            {
                options.XmlEncryptor = new AzureKeyVaultXmlEncryptor(client, keyIdentifier);
            });

            return builder;
        }
    }
}
