// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection;

public class DataProtectionEntityFrameworkTests
{
    [Fact]
    public void CreateRepository_ThrowsIf_ContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>(null, null));
    }

    [Fact]
    public void StoreElement_PersistsData()
    {
        var element = XElement.Parse("<Element1/>");
        var friendlyName = "Element1";
        var key = new DataProtectionKey() { FriendlyName = friendlyName, Xml = element.ToString() };

        var services = GetServices(nameof(StoreElement_PersistsData));
        var service = new EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>(services, NullLoggerFactory.Instance);
        service.StoreElement(element, friendlyName);

        // Use a separate instance of the context to verify correct data was saved to database
        using (var context = services.CreateScope().ServiceProvider.GetRequiredService<DataProtectionKeyContext>())
        {
            Assert.Equal(1, context.DataProtectionKeys.Count());
            Assert.Equal(key.FriendlyName, context.DataProtectionKeys.Single()?.FriendlyName);
            Assert.Equal(key.Xml, context.DataProtectionKeys.Single()?.Xml);
        }
    }

    [Fact]
    public void GetAllElements_ReturnsAllElements()
    {
        var element1 = XElement.Parse("<Element1/>");
        var element2 = XElement.Parse("<Element2/>");

        var services = GetServices(nameof(GetAllElements_ReturnsAllElements));
        var service1 = CreateRepo(services);
        service1.StoreElement(element1, "element1");
        service1.StoreElement(element2, "element2");

        // Use a separate instance of the context to verify correct data was saved to database
        var service2 = CreateRepo(services);
        var elements = service2.GetAllElements();
        Assert.Equal(2, elements.Count);
    }

    private EntityFrameworkCoreXmlRepository<DataProtectionKeyContext> CreateRepo(IServiceProvider services)
        => new EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>(services, NullLoggerFactory.Instance);

    private IServiceProvider GetServices(string dbName)
        => new ServiceCollection()
            .AddDbContext<DataProtectionKeyContext>(o => o.UseInMemoryDatabase(dbName))
            .BuildServiceProvider(validateScopes: true);
}
