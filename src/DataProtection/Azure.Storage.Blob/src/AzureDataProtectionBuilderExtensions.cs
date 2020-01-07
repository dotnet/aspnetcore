// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Azure.Storage;
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
    public static class AzureDataProtectionBuilderExtensions
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

            // The SAS token is present in the query string.

            if (string.IsNullOrEmpty(uriBuilder.Query))
            {
                throw new ArgumentException(
                    message: "URI does not have a SAS token in the query string.",
                    paramName: nameof(blobUri));
            }

            var credentials = new StorageSharedKeyCredential(uriBuilder.AccountName, uriBuilder.Query);
            var blobName = uriBuilder.BlobName;
            uriBuilder.Query = null; // no longer needed
            var blobAbsoluteUri = uriBuilder.ToUri();

            var client = new BlobContainerClient(blobAbsoluteUri, credentials);

            return PersistKeystoAzureBlobStorageInternal(builder, () => client.GetBlobClient(blobName));
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the specified path
        /// in Azure Blob Storage.
        /// </summary>
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="container">The <see cref="BlobContainerClient"/> in which the
        /// key file should be stored.</param>
        /// <param name="blobName">The name of the key file, generally specified
        /// as "[subdir/]keys.xml"</param>
        /// <returns>The value <paramref name="builder"/>.</returns>
        /// <remarks>
        /// The container referenced by <paramref name="container"/> must already exist.
        /// </remarks>
        public static IDataProtectionBuilder PersistKeysToAzureBlobStorage(this IDataProtectionBuilder builder, BlobContainerClient container, string blobName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (blobName == null)
            {
                throw new ArgumentNullException(nameof(blobName));
            }
            return PersistKeystoAzureBlobStorageInternal(builder, () => container.GetBlobClient(blobName));
        }

        // important: the Func passed into this method must return a new instance with each call
        private static IDataProtectionBuilder PersistKeystoAzureBlobStorageInternal(IDataProtectionBuilder builder, Func<BlobClient> blobRefFactory)
        {
            builder.Services.Configure<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new AzureBlobXmlRepository(blobRefFactory);
            });
            return builder;
        }
    }
}
