// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.Internal;

public class KeyManagementOptionsPostSetupTest
{
    private static readonly string keyDir = new DirectoryInfo("/testpath").FullName;
    private static readonly XElement xElement = new("element");

    [Fact]
    public void ConfigureReadOnly()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
        [
            new KeyValuePair<string, string>(KeyManagementOptionsPostSetup.ReadOnlyDataProtectionKeyDirectoryKey, keyDir),
        ]).Build();

        IPostConfigureOptions<KeyManagementOptions> setup = new KeyManagementOptionsPostSetup(config, NullLoggerFactory.Instance);

        var options = new KeyManagementOptions();

        setup.PostConfigure(Options.DefaultName, options);

        AssertReadOnly(options, keyDir);
    }

    [Fact]
    public void ConfigureReadOnly_NonDefaultInstance()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
        [
            new KeyValuePair<string, string>(KeyManagementOptionsPostSetup.ReadOnlyDataProtectionKeyDirectoryKey, keyDir),
        ]).Build();

        IPostConfigureOptions<KeyManagementOptions> setup = new KeyManagementOptionsPostSetup(config, NullLoggerFactory.Instance);

        var options = new KeyManagementOptions();

        setup.PostConfigure(Options.DefaultName + 1, options);

        AssertNotReadOnly(options, keyDir);

        Assert.True(options.AutoGenerateKeys);
    }

    [Fact]
    public void ConfigureReadOnly_EmptyDirPath()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
        [
            new KeyValuePair<string, string>(KeyManagementOptionsPostSetup.ReadOnlyDataProtectionKeyDirectoryKey, ""),
        ]).Build();

        IPostConfigureOptions<KeyManagementOptions> setup = new KeyManagementOptionsPostSetup(config, NullLoggerFactory.Instance);

        var options = new KeyManagementOptions();

        setup.PostConfigure(Options.DefaultName, options);

        AssertNotReadOnly(options, keyDir);

        Assert.True(options.AutoGenerateKeys);
    }

    [Fact]
    public void ConfigureReadOnly_ExplicitRepository()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
        [
            new KeyValuePair<string, string>(KeyManagementOptionsPostSetup.ReadOnlyDataProtectionKeyDirectoryKey, keyDir),
        ]).Build();

        IPostConfigureOptions<KeyManagementOptions> setup = new KeyManagementOptionsPostSetup(config, NullLoggerFactory.Instance);

        var xmlDir = Directory.CreateTempSubdirectory();
        try
        {
            var options = new KeyManagementOptions()
            {
                XmlRepository = new FileSystemXmlRepository(xmlDir, NullLoggerFactory.Instance),
            };

            setup.PostConfigure(Options.DefaultName, options);

            AssertNotReadOnly(options, keyDir);

            Assert.True(options.AutoGenerateKeys);
        }
        finally
        {
            xmlDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ConfigureReadOnly_ExplicitEncryptor()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
        [
            new KeyValuePair<string, string>(KeyManagementOptionsPostSetup.ReadOnlyDataProtectionKeyDirectoryKey, keyDir),
        ]).Build();

        IPostConfigureOptions<KeyManagementOptions> setup = new KeyManagementOptionsPostSetup(config, NullLoggerFactory.Instance);

        var options = new KeyManagementOptions()
        {
            XmlEncryptor = new NullXmlEncryptor(),
        };

        setup.PostConfigure(Options.DefaultName, options);

        AssertNotReadOnly(options, keyDir);

        Assert.True(options.AutoGenerateKeys);
    }

    [Fact]
    public void NotConfigured_NoProperty()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        IPostConfigureOptions<KeyManagementOptions> setup = new KeyManagementOptionsPostSetup(config, NullLoggerFactory.Instance);

        var options = new KeyManagementOptions();

        setup.PostConfigure(Options.DefaultName, options);

        AssertNotReadOnly(options, keyDir);

        Assert.True(options.AutoGenerateKeys);
    }

    [Fact]
    public void NotConfigured_NoIConfiguration()
    {
        IPostConfigureOptions<KeyManagementOptions> setup = new KeyManagementOptionsPostSetup();

        var options = new KeyManagementOptions();

        setup.PostConfigure(Options.DefaultName, options);

        AssertNotReadOnly(options, keyDir);

        Assert.True(options.AutoGenerateKeys);
    }

    private static void AssertReadOnly(KeyManagementOptions options, string keyDir)
    {
        // Effect 1: No key generation
        Assert.False(options.AutoGenerateKeys);

        var repository = options.XmlRepository as FileSystemXmlRepository;
        Assert.NotNull(repository);

        // Effect 2: Location from configuration
        Assert.Equal(keyDir, repository.Directory.FullName);

        // Effect 3: No writing
        Assert.Throws<InvalidOperationException>(() => repository.StoreElement(xElement, friendlyName: null));

        // Effect 4: No key encryption
        Assert.NotNull(options.XmlEncryptor);
        Assert.Throws<InvalidOperationException>(() => options.XmlEncryptor.Encrypt(xElement));
    }

    private static void AssertNotReadOnly(KeyManagementOptions options, string keyDir)
    {
        // Missing effect 1: No key generation
        Assert.True(options.AutoGenerateKeys);

        var repository = options.XmlRepository;
        if (repository is not null)
        {
            // Missing effect 2: Location from configuration
            Assert.NotEqual(keyDir, (repository as FileSystemXmlRepository)?.Directory.FullName);

            // Missing effect 3: No writing
            repository.StoreElement(xElement, friendlyName: null);
        }

        var encryptor = options.XmlEncryptor;
        if (encryptor is not null)
        {
            // Missing effect 4: No key encryption
            options.XmlEncryptor.Encrypt(xElement);
        }
    }
}
