// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.Test
{
    public class EntityFrameworkCoreDataProtectionBuilderExtensionsTests
    {
        [Fact]
        public void PersistKeysToEntityFrameworkCore_UsesEntityFrameworkCoreXmlRepository()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddDbContext<DataProtectionKeyContext>()
                .AddDataProtection()
                .PersistKeysToDbContext<DataProtectionKeyContext>();
            var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
            var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>();
            Assert.IsType<EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>>(keyManagementOptions.Value.XmlRepository);
        }
    }
}
