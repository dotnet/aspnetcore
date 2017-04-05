// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AzureStorage
{
    public class AzureDataProtectionBuilderExtensionsTest
    {
        [Fact]
        public void PersistKeysToAzureBlobStorage_UsesAzureBlobXmlRepository()
        {
            // Arrange
            var container = new CloudBlobContainer(new Uri("http://www.example.com"));
            var serviceCollection = new ServiceCollection();
            var builder = serviceCollection.AddDataProtection();

            // Act
            builder.PersistKeysToAzureBlobStorage(container, "keys.xml");
            var services = serviceCollection.BuildServiceProvider();

            // Assert
            var options = services.GetRequiredService<IOptions<KeyManagementOptions>>();
            Assert.IsType<AzureBlobXmlRepository>(options.Value.XmlRepository);
        }
    }
}
