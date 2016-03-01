// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Win32;

#if !NETSTANDARD1_3 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml
using System.Security.Cryptography.X509Certificates;
#endif

namespace Microsoft.AspNetCore.DataProtection
{
#if !NETSTANDARD1_3
    /// <summary>
    /// Provides access to configuration for the data protection system, which allows the
    /// developer to configure default cryptographic algorithms, key storage locations,
    /// and the mechanism by which keys are protected at rest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the developer changes the at-rest key protection mechanism, it is intended that
    /// he also change the key storage location, and vice versa. For instance, a call to
    /// <see cref="ProtectKeysWithCertificate(string)" /> should generally be accompanied by
    /// a call to <see cref="PersistKeysToFileSystem(DirectoryInfo)"/>, or exceptions may
    /// occur at runtime due to the data protection system not knowing where to persist keys.
    /// </para>
    /// <para>
    /// Similarly, when a developer modifies the default protected payload cryptographic
    /// algorithms, it is intended that he also select an explitiy key storage location.
    /// A call to <see cref="UseCryptographicAlgorithms(AuthenticatedEncryptionOptions)"/>
    /// should therefore generally be paired with a call to <see cref="PersistKeysToFileSystem(DirectoryInfo)"/>,
    /// for example.
    /// </para>
    /// <para>
    /// When the default cryptographic algorithms or at-rest key protection mechanisms are
    /// changed, they only affect <strong>new</strong> keys in the repository. The repository may
    /// contain existing keys that use older algorithms or protection mechanisms.
    /// </para>
    /// </remarks>
#else
    /// <summary>
    /// Provides access to configuration for the data protection system, which allows the
    /// developer to configure default cryptographic algorithms, key storage locations,
    /// and the mechanism by which keys are protected at rest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the developer changes the at-rest key protection mechanism, it is intended that
    /// he also change the key storage location, and vice versa.
    /// </para>
    /// <para>
    /// Similarly, when a developer modifies the default protected payload cryptographic
    /// algorithms, it is intended that he also select an explitiy key storage location.
    /// A call to <see cref="UseCryptographicAlgorithms(AuthenticatedEncryptionOptions)"/>
    /// should therefore generally be paired with a call to <see cref="PersistKeysToFileSystem(DirectoryInfo)"/>,
    /// for example.
    /// </para>
    /// <para>
    /// When the default cryptographic algorithms or at-rest key protection mechanisms are
    /// changed, they only affect <strong>new</strong> keys in the repository. The repository may
    /// contain existing keys that use older algorithms or protection mechanisms.
    /// </para>
    /// </remarks>
#endif
    public class DataProtectionConfiguration
    {
        /// <summary>
        /// Creates a new configuration object linked to a <see cref="IServiceCollection"/>.
        /// </summary>
        public DataProtectionConfiguration(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            Services = services;
        }

        /// <summary>
        /// Provides access to the <see cref="IServiceCollection"/> passed to this object's constructor.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IServiceCollection Services { get; }

        /// <summary>
        /// Registers a <see cref="IKeyEscrowSink"/> to perform escrow before keys are persisted to storage.
        /// </summary>
        /// <param name="sink">The instance of the <see cref="IKeyEscrowSink"/> to register.</param>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// Registrations are additive.
        /// </remarks>
        public DataProtectionConfiguration AddKeyEscrowSink(IKeyEscrowSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            Services.AddSingleton<IKeyEscrowSink>(sink);
            return this;
        }

        /// <summary>
        /// Registers a <see cref="IKeyEscrowSink"/> to perform escrow before keys are persisted to storage.
        /// </summary>
        /// <typeparam name="TImplementation">The concrete type of the <see cref="IKeyEscrowSink"/> to register.</typeparam>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// Registrations are additive. The factory is registered as <see cref="ServiceLifetime.Singleton"/>.
        /// </remarks>
        public DataProtectionConfiguration AddKeyEscrowSink<TImplementation>()
            where TImplementation : class, IKeyEscrowSink
        {
            Services.AddSingleton<IKeyEscrowSink, TImplementation>();
            return this;
        }

        /// <summary>
        /// Registers a <see cref="IKeyEscrowSink"/> to perform escrow before keys are persisted to storage.
        /// </summary>
        /// <param name="factory">A factory that creates the <see cref="IKeyEscrowSink"/> instance.</param>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// Registrations are additive. The factory is registered as <see cref="ServiceLifetime.Singleton"/>.
        /// </remarks>
        public DataProtectionConfiguration AddKeyEscrowSink(Func<IServiceProvider, IKeyEscrowSink> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            Services.AddSingleton<IKeyEscrowSink>(factory);
            return this;
        }

        /// <summary>
        /// Configures miscellaneous global options.
        /// </summary>
        /// <param name="setupAction">A callback that configures the global options.</param>
        /// <returns>The 'this' instance.</returns>
        public DataProtectionConfiguration ConfigureGlobalOptions(Action<DataProtectionOptions> setupAction)
        {
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            Services.Configure(setupAction);
            return this;
        }

        /// <summary>
        /// Configures the data protection system not to generate new keys automatically.
        /// </summary>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// Calling this API corresponds to setting <see cref="KeyManagementOptions.AutoGenerateKeys"/>
        /// to 'false'. See that property's documentation for more information.
        /// </remarks>
        public DataProtectionConfiguration DisableAutomaticKeyGeneration()
        {
            Services.Configure<KeyManagementOptions>(options =>
            {
                options.AutoGenerateKeys = false;
            });
            return this;
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the specified directory.
        /// This path may be on the local machine or may point to a UNC share.
        /// </summary>
        /// <param name="directory">The directory in which to store keys.</param>
        /// <returns>The 'this' instance.</returns>
        public DataProtectionConfiguration PersistKeysToFileSystem(DirectoryInfo directory)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            Use(DataProtectionServiceDescriptors.IXmlRepository_FileSystem(directory));
            return this;
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the Windows registry.
        /// </summary>
        /// <param name="registryKey">The location in the registry where keys should be stored.</param>
        /// <returns>The 'this' instance.</returns>
        public DataProtectionConfiguration PersistKeysToRegistry(RegistryKey registryKey)
        {
            if (registryKey == null)
            {
                throw new ArgumentNullException(nameof(registryKey));
            }

            Use(DataProtectionServiceDescriptors.IXmlRepository_Registry(registryKey));
            return this;
        }

#if !NETSTANDARD1_3 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml

        /// <summary>
        /// Configures keys to be encrypted to a given certificate before being persisted to storage.
        /// </summary>
        /// <param name="certificate">The certificate to use when encrypting keys.</param>
        /// <returns>The 'this' instance.</returns>
        public DataProtectionConfiguration ProtectKeysWithCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            Use(DataProtectionServiceDescriptors.IXmlEncryptor_Certificate(certificate));
            return this;
        }

        /// <summary>
        /// Configures keys to be encrypted to a given certificate before being persisted to storage.
        /// </summary>
        /// <param name="thumbprint">The thumbprint of the certificate to use when encrypting keys.</param>
        /// <returns>The 'this' instance.</returns>
        public DataProtectionConfiguration ProtectKeysWithCertificate(string thumbprint)
        {
            if (thumbprint == null)
            {
                throw new ArgumentNullException(nameof(thumbprint));
            }

            // Make sure the thumbprint corresponds to a valid certificate.
            if (new CertificateResolver().ResolveCertificate(thumbprint) == null)
            {
                throw Error.CertificateXmlEncryptor_CertificateNotFound(thumbprint);
            }

            // ICertificateResolver is necessary for this type to work correctly, so register it
            // if it doesn't already exist.
            Services.TryAdd(DataProtectionServiceDescriptors.ICertificateResolver_Default());
            Use(DataProtectionServiceDescriptors.IXmlEncryptor_Certificate(thumbprint));
            return this;
        }

#endif

        /// <summary>
        /// Configures keys to be encrypted with Windows DPAPI before being persisted to
        /// storage. The encrypted key will only be decryptable by the current Windows user account.
        /// </summary>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// This API is only supported on Windows platforms.
        /// </remarks>
        public DataProtectionConfiguration ProtectKeysWithDpapi()
        {
            return ProtectKeysWithDpapi(protectToLocalMachine: false);
        }

        /// <summary>
        /// Configures keys to be encrypted with Windows DPAPI before being persisted to
        /// storage.
        /// </summary>
        /// <param name="protectToLocalMachine">'true' if the key should be decryptable by any
        /// use on the local machine, 'false' if the key should only be decryptable by the current
        /// Windows user account.</param>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// This API is only supported on Windows platforms.
        /// </remarks>
        public DataProtectionConfiguration ProtectKeysWithDpapi(bool protectToLocalMachine)
        {
            Use(DataProtectionServiceDescriptors.IXmlEncryptor_Dpapi(protectToLocalMachine));
            return this;
        }

        /// <summary>
        /// Configures keys to be encrypted with Windows CNG DPAPI before being persisted
        /// to storage. The keys will be decryptable by the current Windows user account.
        /// </summary>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// See https://msdn.microsoft.com/en-us/library/windows/desktop/hh706794(v=vs.85).aspx
        /// for more information on DPAPI-NG. This API is only supported on Windows 8 / Windows Server 2012 and higher.
        /// </remarks>
        public DataProtectionConfiguration ProtectKeysWithDpapiNG()
        {
            return ProtectKeysWithDpapiNG(
                protectionDescriptorRule: DpapiNGXmlEncryptor.GetDefaultProtectionDescriptorString(),
                flags: DpapiNGProtectionDescriptorFlags.None);
        }

        /// <summary>
        /// Configures keys to be encrypted with Windows CNG DPAPI before being persisted to storage.
        /// </summary>
        /// <param name="protectionDescriptorRule">The descriptor rule string with which to protect the key material.</param>
        /// <param name="flags">Flags that should be passed to the call to 'NCryptCreateProtectionDescriptor'.
        /// The default value of this parameter is <see cref="DpapiNGProtectionDescriptorFlags.None"/>.</param>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// See https://msdn.microsoft.com/en-us/library/windows/desktop/hh769091(v=vs.85).aspx
        /// and https://msdn.microsoft.com/en-us/library/windows/desktop/hh706800(v=vs.85).aspx
        /// for more information on valid values for the the <paramref name="protectionDescriptorRule"/>
        /// and <paramref name="flags"/> arguments.
        /// This API is only supported on Windows 8 / Windows Server 2012 and higher.
        /// </remarks>
        public DataProtectionConfiguration ProtectKeysWithDpapiNG(string protectionDescriptorRule, DpapiNGProtectionDescriptorFlags flags)
        {
            if (protectionDescriptorRule == null)
            {
                throw new ArgumentNullException(nameof(protectionDescriptorRule));
            }

            Use(DataProtectionServiceDescriptors.IXmlEncryptor_DpapiNG(protectionDescriptorRule, flags));
            return this;
        }

        /// <summary>
        /// Sets the unique name of this application within the data protection system.
        /// </summary>
        /// <param name="applicationName">The application name.</param>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// This API corresponds to setting the <see cref="DataProtectionOptions.ApplicationDiscriminator"/> property
        /// to the value of <paramref name="applicationName"/>.
        /// </remarks>
        public DataProtectionConfiguration SetApplicationName(string applicationName)
        {
            return ConfigureGlobalOptions(options =>
            {
                options.ApplicationDiscriminator = applicationName;
            });
        }

        /// <summary>
        /// Sets the default lifetime of keys created by the data protection system.
        /// </summary>
        /// <param name="lifetime">The lifetime (time before expiration) for newly-created keys.
        /// See <see cref="KeyManagementOptions.NewKeyLifetime"/> for more information and
        /// usage notes.</param>
        /// <returns>The 'this' instance.</returns>
        public DataProtectionConfiguration SetDefaultKeyLifetime(TimeSpan lifetime)
        {
            Services.Configure<KeyManagementOptions>(options =>
            {
                options.NewKeyLifetime = lifetime;
            });
            return this;
        }

        /// <summary>
        /// Configures the data protection system to use the specified cryptographic algorithms
        /// by default when generating protected payloads.
        /// </summary>
        /// <param name="options">Information about what cryptographic algorithms should be used.</param>
        /// <returns>The 'this' instance.</returns>
        public DataProtectionConfiguration UseCryptographicAlgorithms(AuthenticatedEncryptionOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return UseCryptographicAlgorithmsCore(options);
        }

        /// <summary>
        /// Configures the data protection system to use custom Windows CNG algorithms.
        /// This API is intended for advanced scenarios where the developer cannot use the
        /// algorithms specified in the <see cref="EncryptionAlgorithm"/> and
        /// <see cref="ValidationAlgorithm"/> enumerations.
        /// </summary>
        /// <param name="options">Information about what cryptographic algorithms should be used.</param>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// This API is only available on Windows.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public DataProtectionConfiguration UseCustomCryptographicAlgorithms(CngCbcAuthenticatedEncryptionOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return UseCryptographicAlgorithmsCore(options);
        }

        /// <summary>
        /// Configures the data protection system to use custom Windows CNG algorithms.
        /// This API is intended for advanced scenarios where the developer cannot use the
        /// algorithms specified in the <see cref="EncryptionAlgorithm"/> and
        /// <see cref="ValidationAlgorithm"/> enumerations.
        /// </summary>
        /// <param name="options">Information about what cryptographic algorithms should be used.</param>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// This API is only available on Windows.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public DataProtectionConfiguration UseCustomCryptographicAlgorithms(CngGcmAuthenticatedEncryptionOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return UseCryptographicAlgorithmsCore(options);
        }

        /// <summary>
        /// Configures the data protection system to use custom algorithms.
        /// This API is intended for advanced scenarios where the developer cannot use the
        /// algorithms specified in the <see cref="EncryptionAlgorithm"/> and
        /// <see cref="ValidationAlgorithm"/> enumerations.
        /// </summary>
        /// <param name="options">Information about what cryptographic algorithms should be used.</param>
        /// <returns>The 'this' instance.</returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public DataProtectionConfiguration UseCustomCryptographicAlgorithms(ManagedAuthenticatedEncryptionOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return UseCryptographicAlgorithmsCore(options);
        }

        private DataProtectionConfiguration UseCryptographicAlgorithmsCore(IInternalAuthenticatedEncryptionOptions options)
        {
            options.Validate(); // perform self-test
            Use(DataProtectionServiceDescriptors.IAuthenticatedEncryptorConfiguration_FromOptions(options));
            return this;
        }

        /// <summary>
        /// Configures the data protection system to use the <see cref="EphemeralDataProtectionProvider"/>
        /// for data protection services.
        /// </summary>
        /// <returns>The 'this' instance.</returns>
        /// <remarks>
        /// If this option is used, payloads protected by the data protection system will
        /// be permanently undecipherable after the application exits.
        /// </remarks>
        public DataProtectionConfiguration UseEphemeralDataProtectionProvider()
        {
            Use(DataProtectionServiceDescriptors.IDataProtectionProvider_Ephemeral());
            return this;
        }

        /*
         * UTILITY ISERVICECOLLECTION METHODS
         */

        private void RemoveAllServicesOfType(Type serviceType)
        {
            // We go backward since we're modifying the collection in-place.
            for (int i = Services.Count - 1; i >= 0; i--)
            {
                if (Services[i]?.ServiceType == serviceType)
                {
                    Services.RemoveAt(i);
                }
            }
        }

        private void Use(ServiceDescriptor descriptor)
        {
            RemoveAllServicesOfType(descriptor.ServiceType);
            Services.Add(descriptor);
        }
    }
}
