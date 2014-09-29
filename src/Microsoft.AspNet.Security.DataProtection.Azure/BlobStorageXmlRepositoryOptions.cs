// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.AspNet.Security.DataProtection.Azure
{
    /// <summary>
    /// Specifies options for configuring an Azure blob storage-based repository.
    /// </summary>
    public class BlobStorageXmlRepositoryOptions
    {
        /// <summary>
        /// The blob storage directory where the key ring will be stored.
        /// </summary>
        public CloudBlobDirectory Directory { get; set; }
    }
}
