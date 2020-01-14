// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection.Azure.Storage.Blob;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Contains Azure-specific extension methods for modifying a
    /// <see cref="IDataProtectionBuilder"/>.
    /// </summary>
    public static class AzureStorageBlobDataProtectionBuilderExtensions
    {
        /// <summary>
        /// Configures the data protection system to persist keys to the specified path
        /// in Azure Blob Storage.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="blobUri">The full URI where the key file should be stored.
        /// The URI must contain the SAS token as a query string parameter.</param>
        /// <returns>The value <paramref name="builder"/>.</returns>
        /// <remarks>
        /// The container referenced by <paramref name="blobUri"/> must already exist.
        /// </remarks>
        public static IDataProtectionBuilder PersistKeysToAzureBlobStorage(this IDataProtectionBuilder builder, Uri blobUri)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (blobUri == null)
            {
                throw new ArgumentNullException(nameof(blobUri));
            }

            var uriBuilder = new BlobUriBuilder(blobUri);
            BlobClient client;

            // The SAS token is present in the query string.
            if (uriBuilder.Sas == null)
            {
                client = new BlobClient(blobUri, new DefaultAzureCredential());
            }
            else
            {
                client = new BlobClient(blobUri);
            }

            return PersistKeystoAzureBlobStorageInternal(builder, client);
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the specified path
        /// in Azure Blob Storage.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="blobUri">The full URI where the key file should be stored.
        /// The URI must contain the SAS token as a query string parameter.</param>
        /// <param name="tokenCredential">The credentials to connect to the blob.</param>
        /// <returns>The value <paramref name="builder"/>.</returns>
        /// <remarks>
        /// The container referenced by <paramref name="blobUri"/> must already exist.
        /// </remarks>
        public static IDataProtectionBuilder PersistKeysToAzureBlobStorage(this IDataProtectionBuilder builder, Uri blobUri, TokenCredential tokenCredential)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (blobUri == null)
            {
                throw new ArgumentNullException(nameof(blobUri));
            }
            if (tokenCredential == null)
            {
                throw new ArgumentNullException(nameof(tokenCredential));
            }

            var client = new BlobClient(blobUri, tokenCredential);

            return PersistKeystoAzureBlobStorageInternal(builder, client);
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the specified path
        /// in Azure Blob Storage.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="blobClient">The <see cref="BlobClient"/> in which the
        /// key file should be stored.</param>
        /// <returns>The value <paramref name="builder"/>.</returns>
        /// <remarks>
        /// The blob referenced by <paramref name="blobClient"/> must already exist.
        /// </remarks>
        public static IDataProtectionBuilder PersistKeysToAzureBlobStorage(this IDataProtectionBuilder builder, BlobClient blobClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (blobClient == null)
            {
                throw new ArgumentNullException(nameof(blobClient));
            }
            return PersistKeystoAzureBlobStorageInternal(builder, blobClient);
        }

        private static IDataProtectionBuilder PersistKeystoAzureBlobStorageInternal(IDataProtectionBuilder builder, BlobClient blobClient)
        {
            builder.Services.Configure<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new AzureBlobXmlRepository(blobClient);
            });
            return builder;
        }
    }
}
