// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Azure.Storage.Blob
{
    public class AzureDataProtectionBuilderExtensionsTest
    {
        [Fact]
        public void PersistKeysToAzureBlobStorage_UsesAzureBlobXmlRepository()
        {
            // Arrange
            var client = new BlobClient(new Uri("http://www.example.com"));
            var serviceCollection = new ServiceCollection();
            var builder = serviceCollection.AddDataProtection();

            // Act
            builder.PersistKeysToAzureBlobStorage(client);
            var services = serviceCollection.BuildServiceProvider();

            // Assert
            var options = services.GetRequiredService<IOptions<KeyManagementOptions>>();
            Assert.IsType<AzureBlobXmlRepository>(options.Value.XmlRepository);
        }
    }
}
