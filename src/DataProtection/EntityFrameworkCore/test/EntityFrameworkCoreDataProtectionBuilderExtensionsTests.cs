// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.Test;

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
