// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    public static partial class AzureDataProtectionBuilderExtensions
    {
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToAzureBlobStorage(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.Azure.Storage.Blob.CloudBlobContainer container, string blobName) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToAzureBlobStorage(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.Azure.Storage.Blob.CloudBlockBlob blobReference) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToAzureBlobStorage(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, Microsoft.Azure.Storage.CloudStorageAccount storageAccount, string relativePath) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToAzureBlobStorage(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, System.Uri blobUri) { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.AzureStorage
{
    public sealed partial class AzureBlobXmlRepository : Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository
    {
        public AzureBlobXmlRepository(System.Func<Microsoft.Azure.Storage.Blob.ICloudBlob> blobRefFactory) { }
        public System.Collections.Generic.IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() { throw null; }
        public void StoreElement(System.Xml.Linq.XElement element, string friendlyName) { }
    }
}
