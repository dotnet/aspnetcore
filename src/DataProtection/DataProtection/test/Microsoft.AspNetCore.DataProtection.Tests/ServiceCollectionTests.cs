// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection;

public class ServiceCollectionTests
{
    [Fact]
    public void AddsOptions()
    {
        var services = new ServiceCollection()
            .AddDataProtection()
            .Services
            .BuildServiceProvider();

        Assert.NotNull(services.GetService<IOptions<DataProtectionOptions>>());
    }

    [Fact]
    public void DoesNotOverrideLogging()
    {
        var services1 = new ServiceCollection()
            .AddLogging()
            .AddDataProtection()
            .Services
            .BuildServiceProvider();

        var services2 = new ServiceCollection()
            .AddDataProtection()
            .Services
            .AddLogging()
            .BuildServiceProvider();

        Assert.Equal(
            services1.GetRequiredService<ILoggerFactory>().GetType(),
            services2.GetRequiredService<ILoggerFactory>().GetType());
    }

    [Fact]
    public void CanResolveAllRegisteredServices()
    {
        var serviceCollection = new ServiceCollection()
            .AddDataProtection()
            .Services;
        var services = serviceCollection.BuildServiceProvider(validateScopes: true);

        Assert.Null(services.GetService<ILoggerFactory>());

        foreach (var descriptor in serviceCollection)
        {
            if (descriptor.ServiceType.Assembly.GetName().Name == "Microsoft.Extensions.Options")
            {
                // ignore any descriptors added by the call to .AddOptions()
                continue;
            }

            Assert.NotNull(services.GetService(descriptor.ServiceType));
        }
    }

    [Fact]
    public void ReadOnlyDataProtectionKeyDirectory()
    {
        var keyDir = new DirectoryInfo("/testpath").FullName;

        var config = new ConfigurationBuilder().AddInMemoryCollection(
        [
            new KeyValuePair<string, string>(KeyManagementOptionsPostSetup.ReadOnlyDataProtectionKeyDirectoryKey, keyDir),
        ]).Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddDataProtection()
            .Services
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

        // Effect 1: No key generation
        Assert.False(options.AutoGenerateKeys);

        var repository = options.XmlRepository as FileSystemXmlRepository;
        Assert.NotNull(repository);

        // Effect 2: Location from configuration
        Assert.Equal(keyDir, repository.Directory.FullName);

        var xElement = new XElement("element");

        // Effect 3: No writing
        Assert.Throws<InvalidOperationException>(() => repository.StoreElement(xElement, friendlyName: null));

        // Effect 4: No key encryption
        Assert.NotNull(options.XmlEncryptor);
        Assert.Throws<InvalidOperationException>(() => options.XmlEncryptor.Encrypt(xElement));
    }

    [Fact]
    public void NoReadOnlyDataProtectionKeyDirectory()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddDataProtection()
            .Services
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

        // Missing effect 1: No key generation
        Assert.True(options.AutoGenerateKeys);

        // KeyManagementOptionsPostSetupTest covers other missing effects
    }
}
