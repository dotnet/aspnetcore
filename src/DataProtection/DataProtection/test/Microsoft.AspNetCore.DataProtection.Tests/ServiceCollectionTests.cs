// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

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

    [Fact]
    public void CanUnprotectPayloadCreatedBy10_0_5()
    {
        var keyRing = XElement.Parse("""
            <key id="ead9607a-7754-4369-a7f4-6696e5713ced" version="1">
              <creationDate>2026-04-16T16:40:09.6237156Z</creationDate>
              <activationDate>2026-04-16T16:40:09.6237156Z</activationDate>
              <expirationDate>2026-07-15T16:40:09.6237156Z</expirationDate>
              <descriptor deserializerType="Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60">
                <descriptor>
                  <encryption algorithm="AES_256_CBC" />
                  <validation algorithm="HMACSHA256" />
                  <masterKey p4:requiresEncryption="true" xmlns:p4="http://schemas.asp.net/2015/03/dataProtection">
                    <value>iEQ2XFkBUwAnEotfX5b+ZWrqKdJFlxJ4bDuJqljdR6KNolzKxa/1hro1PZg4tsb6CJsOZ/fCA/8VKGX+bXHq4Q==</value>
                  </masterKey>
                </descriptor>
              </descriptor>
            </key>
            """);
        const string protectedPayload = "CfDJ8Hpg2epUd2lDp_RmluVxPO1RuoXCtT528WJ_4cy3XD_uhLBk9GBkYOQF-iIWlloCb4CP7rX609LnU7BiXXgZU1fKvNGfjwau5YJRIXcvDyXDnvn1kyRDojEYPc5Ic4WQgzBnpZrf2qiI4JrX9nkqCew";

        var services = new ServiceCollection()
            .AddDataProtection()
            .SetApplicationName("repro-app")
            .Services
            .Configure<KeyManagementOptions>(options =>
            {
                options.AutoGenerateKeys = false;
                options.XmlRepository = new SingleElementXmlRepository(keyRing);
            })
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IDataProtectionProvider>();
        var protector = provider.CreateProtector("purpose");

        var unprotectedPayload = protector.Unprotect(protectedPayload);

        Assert.Equal("hello from 10.0.5", unprotectedPayload);
    }

    private sealed class SingleElementXmlRepository : IXmlRepository
    {
        private readonly XElement _element;

        public SingleElementXmlRepository(XElement element)
        {
            _element = new XElement(element);
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return new[] { new XElement(_element) };
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            throw new NotSupportedException();
        }
    }
}
