// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.Internal;

/// <summary>
/// Performs additional <see cref="KeyManagementOptions" /> configuration, after the user's configuration has been applied.
/// </summary>
/// <remarks>
/// In practice, this type is used to set key management to readonly mode if an environment variable is set and the user
/// has not explicitly configured data protection.
/// </remarks>
internal sealed class KeyManagementOptionsPostSetup : IPostConfigureOptions<KeyManagementOptions>
{
    /// <remarks>
    /// Settable as `ReadOnlyDataProtectionKeyDirectory`, `DOTNET_ReadOnlyDataProtectionKeyDirectory`,
    /// or `ASPNETCORE_ReadOnlyDataProtectionKeyDirectory`, in descending order of precedence.
    /// </remarks>
    internal const string ReadOnlyDataProtectionKeyDirectoryKey = "ReadOnlyDataProtectionKeyDirectory";

    private readonly string? _keyDirectoryPath;
    private readonly ILoggerFactory? _loggerFactory; // Null iff _keyDirectoryPath is null
    private readonly ILogger<KeyManagementOptionsPostSetup>? _logger; // Null iff _keyDirectoryPath is null

    public KeyManagementOptionsPostSetup()
    {
        // If there's no IConfiguration, there's no _keyDirectoryPath and this type will do nothing.
        // This is mostly a convenience for tests since ASP.NET Core apps will have an IConfiguration.
    }

    public KeyManagementOptionsPostSetup(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var dirPath = configuration[ReadOnlyDataProtectionKeyDirectoryKey];
        if (string.IsNullOrEmpty(dirPath))
        {
            return;
        }

        _keyDirectoryPath = dirPath;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<KeyManagementOptionsPostSetup>();
    }

    void IPostConfigureOptions<KeyManagementOptions>.PostConfigure(string? name, KeyManagementOptions options)
    {
        if (_keyDirectoryPath is null)
        {
            // There's no logger, so we couldn't log if we wanted to
            return;
        }

        var logger = _logger!;

        if (name != Options.DefaultName)
        {
            logger.IgnoringReadOnlyConfigurationForNonDefaultOptions(ReadOnlyDataProtectionKeyDirectoryKey, name);
            return;
        }

        // If Data Protection has not been configured, then set it up according to the environment variable
        if (options is { XmlRepository: null, XmlEncryptor: null })
        {
            var keyDirectory = new DirectoryInfo(_keyDirectoryPath);

            logger.UsingReadOnlyKeyConfiguration(keyDirectory.FullName);

            options.AutoGenerateKeys = false;
            options.XmlEncryptor = InvalidEncryptor.Instance;
            options.XmlRepository = new ReadOnlyFileSystemXmlRepository(keyDirectory, _loggerFactory!);
        }
        else if (options.XmlRepository is not null)
        {
            logger.NotUsingReadOnlyKeyConfigurationBecauseOfRepository();
        }
        else
        {
            logger.NotUsingReadOnlyKeyConfigurationBecauseOfEncryptor();
        }
    }

    private sealed class InvalidEncryptor : IXmlEncryptor
    {
        public static readonly IXmlEncryptor Instance = new InvalidEncryptor();

        private InvalidEncryptor()
        {
        }

        EncryptedXmlInfo IXmlEncryptor.Encrypt(XElement plaintextElement)
        {
            throw new InvalidOperationException("Keys access is set up as read-only, so nothing should be encrypting");
        }
    }

    private sealed class ReadOnlyFileSystemXmlRepository : FileSystemXmlRepository
    {
        public ReadOnlyFileSystemXmlRepository(DirectoryInfo directory, ILoggerFactory loggerFactory)
            : base(directory, loggerFactory)
        {
        }

        public override void StoreElement(XElement element, string friendlyName)
        {
            throw new InvalidOperationException("Keys access is set up as read-only, so nothing should be storing keys");
        }
    }
}
