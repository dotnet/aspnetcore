// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Extensions for configuring data protection using an <see cref="IDataProtectionBuilder"/>.
/// </summary>
public static class DataProtectionBuilderExtensions
{
    /// <summary>
    /// Sets the unique name of this application within the data protection system.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="applicationName">The application name.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// This API corresponds to setting the <see cref="DataProtectionOptions.ApplicationDiscriminator"/> property
    /// to the value of <paramref name="applicationName"/>.
    /// </remarks>
    public static IDataProtectionBuilder SetApplicationName(this IDataProtectionBuilder builder, string applicationName)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        builder.Services.Configure<DataProtectionOptions>(options =>
        {
            options.ApplicationDiscriminator = applicationName;
        });

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IKeyEscrowSink"/> to perform escrow before keys are persisted to storage.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="sink">The instance of the <see cref="IKeyEscrowSink"/> to register.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// Registrations are additive.
    /// </remarks>
    public static IDataProtectionBuilder AddKeyEscrowSink(this IDataProtectionBuilder builder, IKeyEscrowSink sink)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(sink);

        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            options.KeyEscrowSinks.Add(sink);
        });

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IKeyEscrowSink"/> to perform escrow before keys are persisted to storage.
    /// </summary>
    /// <typeparam name="TImplementation">The concrete type of the <see cref="IKeyEscrowSink"/> to register.</typeparam>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// Registrations are additive. The factory is registered as <see cref="ServiceLifetime.Singleton"/>.
    /// </remarks>
    public static IDataProtectionBuilder AddKeyEscrowSink<TImplementation>(this IDataProtectionBuilder builder)
        where TImplementation : class, IKeyEscrowSink
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var implementationInstance = services.GetRequiredService<TImplementation>();
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.KeyEscrowSinks.Add(implementationInstance);
            });
        });

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IKeyEscrowSink"/> to perform escrow before keys are persisted to storage.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="factory">A factory that creates the <see cref="IKeyEscrowSink"/> instance.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// Registrations are additive. The factory is registered as <see cref="ServiceLifetime.Singleton"/>.
    /// </remarks>
    public static IDataProtectionBuilder AddKeyEscrowSink(this IDataProtectionBuilder builder, Func<IServiceProvider, IKeyEscrowSink> factory)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(factory);

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var instance = factory(services);
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.KeyEscrowSinks.Add(instance);
            });
        });

        return builder;
    }

    /// <summary>
    /// Configures the key management options for the data protection system.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="setupAction">An <see cref="Action{KeyManagementOptions}"/> to configure the provided <see cref="KeyManagementOptions"/>.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder AddKeyManagementOptions(this IDataProtectionBuilder builder, Action<KeyManagementOptions> setupAction)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(setupAction);

        builder.Services.Configure(setupAction);
        return builder;
    }

    /// <summary>
    /// Configures the data protection system not to generate new keys automatically.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// Calling this API corresponds to setting <see cref="KeyManagementOptions.AutoGenerateKeys"/>
    /// to 'false'. See that property's documentation for more information.
    /// </remarks>
    public static IDataProtectionBuilder DisableAutomaticKeyGeneration(this IDataProtectionBuilder builder)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            options.AutoGenerateKeys = false;
        });
        return builder;
    }

    /// <summary>
    /// Configures the data protection system to persist keys to the specified directory.
    /// This path may be on the local machine or may point to a UNC share.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="directory">The directory in which to store keys.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder PersistKeysToFileSystem(this IDataProtectionBuilder builder, DirectoryInfo directory)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(directory);

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new FileSystemXmlRepository(directory, loggerFactory);
            });
        });

        return builder;
    }

    /// <summary>
    /// Configures the data protection system to persist keys to the Windows registry.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="registryKey">The location in the registry where keys should be stored.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    [SupportedOSPlatform("windows")]
    public static IDataProtectionBuilder PersistKeysToRegistry(this IDataProtectionBuilder builder, RegistryKey registryKey)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(registryKey);

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new RegistryXmlRepository(registryKey, loggerFactory);
            });
        });

        return builder;
    }

    /// <summary>
    /// Configures keys to be encrypted to a given certificate before being persisted to storage.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="certificate">The certificate to use when encrypting keys.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder ProtectKeysWithCertificate(this IDataProtectionBuilder builder, X509Certificate2 certificate)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(certificate);

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlEncryptor = new CertificateXmlEncryptor(certificate, loggerFactory);
            });
        });

        builder.Services.Configure<XmlKeyDecryptionOptions>(o => o.AddKeyDecryptionCertificate(certificate));

        return builder;
    }

    /// <summary>
    /// Configures keys to be encrypted to a given certificate before being persisted to storage.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="thumbprint">The thumbprint of the certificate to use when encrypting keys.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder ProtectKeysWithCertificate(this IDataProtectionBuilder builder, string thumbprint)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(thumbprint);

        // Make sure the thumbprint corresponds to a valid certificate.
        if (new CertificateResolver().ResolveCertificate(thumbprint) == null)
        {
            throw Error.CertificateXmlEncryptor_CertificateNotFound(thumbprint);
        }

        // ICertificateResolver is necessary for this type to work correctly, so register it
        // if it doesn't already exist.
        builder.Services.TryAddSingleton<ICertificateResolver, CertificateResolver>();

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var certificateResolver = services.GetRequiredService<ICertificateResolver>();
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlEncryptor = new CertificateXmlEncryptor(thumbprint, certificateResolver, loggerFactory);
            });
        });

        return builder;
    }

    /// <summary>
    /// Configures certificates which can be used to decrypt keys loaded from storage.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="certificates">Certificates that can be used to decrypt key data.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder UnprotectKeysWithAnyCertificate(this IDataProtectionBuilder builder, params X509Certificate2[] certificates)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        builder.Services.Configure<XmlKeyDecryptionOptions>(o =>
        {
            if (certificates != null)
            {
                foreach (var certificate in certificates)
                {
                    o.AddKeyDecryptionCertificate(certificate);
                }
            }
        });

        return builder;
    }

    /// <summary>
    /// Configures keys to be encrypted with Windows DPAPI before being persisted to
    /// storage. The encrypted key will only be decryptable by the current Windows user account.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// This API is only supported on Windows platforms.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public static IDataProtectionBuilder ProtectKeysWithDpapi(this IDataProtectionBuilder builder)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        return builder.ProtectKeysWithDpapi(protectToLocalMachine: false);
    }

    /// <summary>
    /// Configures keys to be encrypted with Windows DPAPI before being persisted to
    /// storage.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="protectToLocalMachine">'true' if the key should be decryptable by any
    /// use on the local machine, 'false' if the key should only be decryptable by the current
    /// Windows user account.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// This API is only supported on Windows platforms.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public static IDataProtectionBuilder ProtectKeysWithDpapi(this IDataProtectionBuilder builder, bool protectToLocalMachine)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                CryptoUtil.AssertPlatformIsWindows();
                options.XmlEncryptor = new DpapiXmlEncryptor(protectToLocalMachine, loggerFactory);
            });
        });

        return builder;
    }

    /// <summary>
    /// Configures keys to be encrypted with Windows CNG DPAPI before being persisted
    /// to storage. The keys will be decryptable by the current Windows user account.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// See <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/hh706794(v=vs.85).aspx"/>
    /// for more information on DPAPI-NG. This API is only supported on Windows 8 / Windows Server 2012 and higher.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public static IDataProtectionBuilder ProtectKeysWithDpapiNG(this IDataProtectionBuilder builder)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        return builder.ProtectKeysWithDpapiNG(
            protectionDescriptorRule: DpapiNGXmlEncryptor.GetDefaultProtectionDescriptorString(),
            flags: DpapiNGProtectionDescriptorFlags.None);
    }

    /// <summary>
    /// Configures keys to be encrypted with Windows CNG DPAPI before being persisted to storage.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="protectionDescriptorRule">The descriptor rule string with which to protect the key material.</param>
    /// <param name="flags">Flags that should be passed to the call to 'NCryptCreateProtectionDescriptor'.
    /// The default value of this parameter is <see cref="DpapiNGProtectionDescriptorFlags.None"/>.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// See <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/hh769091(v=vs.85).aspx"/>
    /// and <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/hh706800(v=vs.85).aspx"/>
    /// for more information on valid values for the the <paramref name="protectionDescriptorRule"/>
    /// and <paramref name="flags"/> arguments.
    /// This API is only supported on Windows 8 / Windows Server 2012 and higher.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public static IDataProtectionBuilder ProtectKeysWithDpapiNG(this IDataProtectionBuilder builder, string protectionDescriptorRule, DpapiNGProtectionDescriptorFlags flags)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(protectionDescriptorRule);

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                CryptoUtil.AssertPlatformIsWindows8OrLater();
                options.XmlEncryptor = new DpapiNGXmlEncryptor(protectionDescriptorRule, flags, loggerFactory);
            });
        });

        return builder;
    }

    /// <summary>
    /// Sets the default lifetime of keys created by the data protection system.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="lifetime">The lifetime (time before expiration) for newly-created keys.
    /// See <see cref="KeyManagementOptions.NewKeyLifetime"/> for more information and
    /// usage notes.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder SetDefaultKeyLifetime(this IDataProtectionBuilder builder, TimeSpan lifetime)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        if (lifetime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(Resources.FormatLifetimeMustNotBeNegative(nameof(lifetime)));
        }

        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            options.NewKeyLifetime = lifetime;
        });

        return builder;
    }

    /// <summary>
    /// Configures the data protection system to use the specified cryptographic algorithms
    /// by default when generating protected payloads.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="configuration">Information about what cryptographic algorithms should be used.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    public static IDataProtectionBuilder UseCryptographicAlgorithms(this IDataProtectionBuilder builder, AuthenticatedEncryptorConfiguration configuration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(configuration);

        return UseCryptographicAlgorithmsCore(builder, configuration);
    }

    /// <summary>
    /// Configures the data protection system to use custom Windows CNG algorithms.
    /// This API is intended for advanced scenarios where the developer cannot use the
    /// algorithms specified in the <see cref="EncryptionAlgorithm"/> and
    /// <see cref="ValidationAlgorithm"/> enumerations.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="configuration">Information about what cryptographic algorithms should be used.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// This API is only available on Windows.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [SupportedOSPlatform("windows")]
    public static IDataProtectionBuilder UseCustomCryptographicAlgorithms(this IDataProtectionBuilder builder, CngCbcAuthenticatedEncryptorConfiguration configuration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(configuration);

        return UseCryptographicAlgorithmsCore(builder, configuration);
    }

    /// <summary>
    /// Configures the data protection system to use custom Windows CNG algorithms.
    /// This API is intended for advanced scenarios where the developer cannot use the
    /// algorithms specified in the <see cref="EncryptionAlgorithm"/> and
    /// <see cref="ValidationAlgorithm"/> enumerations.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="configuration">Information about what cryptographic algorithms should be used.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// This API is only available on Windows.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [SupportedOSPlatform("windows")]
    public static IDataProtectionBuilder UseCustomCryptographicAlgorithms(this IDataProtectionBuilder builder, CngGcmAuthenticatedEncryptorConfiguration configuration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(configuration);

        return UseCryptographicAlgorithmsCore(builder, configuration);
    }

    /// <summary>
    /// Configures the data protection system to use custom algorithms.
    /// This API is intended for advanced scenarios where the developer cannot use the
    /// algorithms specified in the <see cref="EncryptionAlgorithm"/> and
    /// <see cref="ValidationAlgorithm"/> enumerations.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <param name="configuration">Information about what cryptographic algorithms should be used.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static IDataProtectionBuilder UseCustomCryptographicAlgorithms(this IDataProtectionBuilder builder, ManagedAuthenticatedEncryptorConfiguration configuration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);
        ArgumentNullThrowHelper.ThrowIfNull(configuration);

        return UseCryptographicAlgorithmsCore(builder, configuration);
    }

    private static IDataProtectionBuilder UseCryptographicAlgorithmsCore(IDataProtectionBuilder builder, AlgorithmConfiguration configuration)
    {
        ((IInternalAlgorithmConfiguration)configuration).Validate(); // perform self-test

        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            options.AuthenticatedEncryptorConfiguration = configuration;
        });

        return builder;
    }

    /// <summary>
    /// Configures the data protection system to use the <see cref="EphemeralDataProtectionProvider"/>
    /// for data protection services.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
    /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
    /// <remarks>
    /// If this option is used, payloads protected by the data protection system will
    /// be permanently undecipherable after the application exits.
    /// </remarks>
    public static IDataProtectionBuilder UseEphemeralDataProtectionProvider(this IDataProtectionBuilder builder)
    {
        ArgumentNullThrowHelper.ThrowIfNull(builder);

        builder.Services.Replace(ServiceDescriptor.Singleton<IDataProtectionProvider, EphemeralDataProtectionProvider>());

        return builder;
    }
}
