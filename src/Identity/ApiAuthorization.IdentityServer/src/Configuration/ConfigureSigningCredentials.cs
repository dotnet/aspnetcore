// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

internal sealed class ConfigureSigningCredentials : IConfigureOptions<ApiAuthorizationOptions>
{
    // We need to cast the underlying int value of the EphemeralKeySet to X509KeyStorageFlags
    // due to the fact that is not part of .NET Standard. This value is only used with non-windows
    // platforms (all .NET Core) for which the value is defined on the underlying platform.
    private const X509KeyStorageFlags UnsafeEphemeralKeySet = (X509KeyStorageFlags)32;
    private const string DefaultTempKeyRelativePath = "obj/tempkey.json";
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigureSigningCredentials> _logger;

    public ConfigureSigningCredentials(
        IConfiguration configuration,
        ILogger<ConfigureSigningCredentials> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void Configure(ApiAuthorizationOptions options)
    {
        var key = LoadKey();
        if (key != null)
        {
            options.SigningCredential = key;
        }
    }

    public SigningCredentials LoadKey()
    {
        // We can't know for sure if there was a configuration section explicitly defined.
        // Check if the current configuration has any children and avoid failing if that's the case.
        // This will avoid failing when no configuration has been specified but will still fail if partial data
        // was defined.
        if (!_configuration.GetChildren().Any())
        {
            return null;
        }

        var key = new KeyDefinition()
        {
            Type = _configuration[nameof(KeyDefinition.Type)],
            FilePath = _configuration[nameof(KeyDefinition.FilePath)],
            Password = _configuration[nameof(KeyDefinition.Password)],
            Name = _configuration[nameof(KeyDefinition.Name)],
            StoreLocation = _configuration[nameof(KeyDefinition.StoreLocation)],
            StoreName = _configuration[nameof(KeyDefinition.StoreName)],
            StorageFlags = _configuration[nameof(KeyDefinition.StorageFlags)]
        };

        if (bool.TryParse(_configuration[nameof(KeyDefinition.Persisted)], out var value))
        {
            key.Persisted = value;
        }

        switch (key.Type)
        {
            case KeySources.Development:
                var developmentKeyPath = Path.Combine(Directory.GetCurrentDirectory(), key.FilePath ?? DefaultTempKeyRelativePath);
                var createIfMissing = key.Persisted ?? true;
                _logger.LogInformation(LoggerEventIds.DevelopmentKeyLoaded, "Loading development key at '{developmentKeyPath}'.", developmentKeyPath);
                var developmentKey = new RsaSecurityKey(SigningKeysLoader.LoadDevelopment(developmentKeyPath, createIfMissing))
                {
                    KeyId = "Development"
                };
                return new SigningCredentials(developmentKey, "RS256");
            case KeySources.File:
                var pfxPath = Path.Combine(Directory.GetCurrentDirectory(), key.FilePath);
                var storageFlags = GetStorageFlags(key);
                _logger.LogInformation(LoggerEventIds.CertificateLoadedFromFile, "Loading certificate file at '{CertificatePath}' with storage flags '{CertificateStorageFlags}'.", pfxPath, key.StorageFlags);
                return new SigningCredentials(new X509SecurityKey(SigningKeysLoader.LoadFromFile(pfxPath, key.Password, storageFlags)), "RS256");
            case KeySources.Store:
                if (!Enum.TryParse<StoreLocation>(key.StoreLocation, out var storeLocation))
                {
                    throw new InvalidOperationException($"Invalid certificate store location '{key.StoreLocation}'.");
                }
                _logger.LogInformation(LoggerEventIds.CertificateLoadedFromStore, "Loading certificate with subject '{CertificateSubject}' in '{CertificateStoreLocation}\\{CertificateStoreName}'.", key.Name, key.StoreLocation, key.StoreName);
                return new SigningCredentials(new X509SecurityKey(SigningKeysLoader.LoadFromStoreCert(key.Name, key.StoreName, storeLocation, GetCurrentTime())), "RS256");
            default:
                throw new InvalidOperationException($"Invalid key type '{key.Type ?? "(null)"}'.");
        }
    }

    // for testing purposes only
    internal static DateTimeOffset GetCurrentTime() => DateTimeOffset.UtcNow;

    private static X509KeyStorageFlags GetStorageFlags(KeyDefinition key)
    {
        var defaultFlags = OperatingSystem.IsLinux() ?
            UnsafeEphemeralKeySet : (OperatingSystem.IsMacOS() ? X509KeyStorageFlags.PersistKeySet :
            X509KeyStorageFlags.DefaultKeySet);

        if (key.StorageFlags == null)
        {
            return defaultFlags;
        }

        var flagsList = key.StorageFlags.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (flagsList.Length == 0)
        {
            return defaultFlags;
        }

        var result = ParseCurrentFlag(flagsList[0]);
        foreach (var flag in flagsList.Skip(1))
        {
            result |= ParseCurrentFlag(flag);
        }

        return result;

        static X509KeyStorageFlags ParseCurrentFlag(string candidate)
        {
            if (Enum.TryParse<X509KeyStorageFlags>(candidate, out var flag))
            {
                return flag;
            }
            else
            {
                throw new InvalidOperationException($"Invalid storage flag '{candidate}'");
            }
        }
    }
}
