// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNet.DataProtection.KeyManagement;
using Microsoft.AspNet.DataProtection.Repositories;
using Microsoft.AspNet.DataProtection.XmlEncryption;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

#if !DOTNET5_4 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml
using System.Security.Cryptography.X509Certificates;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Default <see cref="ServiceDescriptor"/> instances for the Data Protection system.
    /// </summary>
    internal static class DataProtectionServiceDescriptors
    {
        /// <summary>
        /// An <see cref="IConfigureOptions{DataProtectionOptions}"/> backed by the host-provided defaults.
        /// </summary>
        public static ServiceDescriptor ConfigureOptions_DataProtectionOptions()
        {
            return ServiceDescriptor.Transient<IConfigureOptions<DataProtectionOptions>>(services =>
            {
                return new ConfigureOptions<DataProtectionOptions>(options =>
                {
                    options.ApplicationDiscriminator = services.GetApplicationUniqueIdentifier();
                });
            });
        }

        /// <summary>
        /// An <see cref="IConfigureOptions{KeyManagementOptions}"/> where the key lifetime is specified explicitly.
        /// </summary>

        public static ServiceDescriptor ConfigureOptions_DefaultKeyLifetime(int numDays)
        {
            return ServiceDescriptor.Transient<IConfigureOptions<KeyManagementOptions>>(services =>
            {
                return new ConfigureOptions<KeyManagementOptions>(options =>
                {
                    options.NewKeyLifetime = TimeSpan.FromDays(numDays);
                });
            });
        }

        /// <summary>
        /// An <see cref="IAuthenticatedEncryptorConfiguration"/> backed by default algorithmic options.
        /// </summary>
        public static ServiceDescriptor IAuthenticatedEncryptorConfiguration_Default()
        {
            return IAuthenticatedEncryptorConfiguration_FromOptions(new AuthenticatedEncryptionOptions());
        }

        /// <summary>
        /// An <see cref="IAuthenticatedEncryptorConfiguration"/> backed by an <see cref="IInternalAuthenticatedEncryptionOptions"/>.
        /// </summary>
        public static ServiceDescriptor IAuthenticatedEncryptorConfiguration_FromOptions(IInternalAuthenticatedEncryptionOptions options)
        {
            return ServiceDescriptor.Singleton<IAuthenticatedEncryptorConfiguration>(options.ToConfiguration);
        }

#if !DOTNET5_4 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml
        /// <summary>
        /// An <see cref="ICertificateResolver"/> backed by the default implementation.
        /// </summary>
        public static ServiceDescriptor ICertificateResolver_Default()
        {
            return ServiceDescriptor.Singleton<ICertificateResolver, CertificateResolver>();
        }
#endif

        /// <summary>
        /// An <see cref="IDataProtectionProvider"/> backed by the default keyring.
        /// </summary>
        public static ServiceDescriptor IDataProtectionProvider_Default()
        {
            return ServiceDescriptor.Singleton<IDataProtectionProvider>(
                services => DataProtectionProviderFactory.GetProviderFromServices(
                    options: services.GetRequiredService<IOptions<DataProtectionOptions>>().Value,
                    services: services,
                    mustCreateImmediately: true /* this is the ultimate fallback */));
        }

        /// <summary>
        /// An ephemeral <see cref="IDataProtectionProvider"/>.
        /// </summary>
        public static ServiceDescriptor IDataProtectionProvider_Ephemeral()
        {
            return ServiceDescriptor.Singleton<IDataProtectionProvider>(services => new EphemeralDataProtectionProvider(services));
        }

        /// <summary>
        /// An <see cref="IKeyEscrowSink"/> backed by a given implementation type.
        /// </summary>
        /// <remarks>
        /// The implementation type name is provided as a string so that we can provide activation services.
        /// </remarks>
        public static ServiceDescriptor IKeyEscrowSink_FromTypeName(string implementationTypeName)
        {
            return ServiceDescriptor.Singleton<IKeyEscrowSink>(services => services.GetActivator().CreateInstance<IKeyEscrowSink>(implementationTypeName));
        }

        /// <summary>
        /// An <see cref="IKeyManager"/> backed by the default XML key manager.
        /// </summary>
        public static ServiceDescriptor IKeyManager_Default()
        {
            return ServiceDescriptor.Singleton<IKeyManager>(services => new XmlKeyManager(services));
        }

#if !DOTNET5_4 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml

        /// <summary>
        /// An <see cref="IXmlEncryptor"/> backed by an X.509 certificate.
        /// </summary>
        public static ServiceDescriptor IXmlEncryptor_Certificate(X509Certificate2 certificate)
        {
            return ServiceDescriptor.Singleton<IXmlEncryptor>(services => new CertificateXmlEncryptor(certificate, services));
        }

        /// <summary>
        /// An <see cref="IXmlEncryptor"/> backed by an X.509 certificate.
        /// </summary>
        public static ServiceDescriptor IXmlEncryptor_Certificate(string thumbprint)
        {
            return ServiceDescriptor.Singleton<IXmlEncryptor>(services => new CertificateXmlEncryptor(
                thumbprint: thumbprint,
                certificateResolver: services.GetRequiredService<ICertificateResolver>(),
                services: services));
        }

#endif

        /// <summary>
        /// An <see cref="IXmlEncryptor"/> backed by DPAPI.
        /// </summary>
        public static ServiceDescriptor IXmlEncryptor_Dpapi(bool protectToMachine)
        {
            CryptoUtil.AssertPlatformIsWindows();
            return ServiceDescriptor.Singleton<IXmlEncryptor>(services => new DpapiXmlEncryptor(protectToMachine, services));
        }

        /// <summary>
        /// An <see cref="IXmlEncryptor"/> backed by DPAPI-NG.
        /// </summary>
        public static ServiceDescriptor IXmlEncryptor_DpapiNG(string protectionDescriptorRule, DpapiNGProtectionDescriptorFlags flags)
        {
            CryptoUtil.AssertPlatformIsWindows8OrLater();
            return ServiceDescriptor.Singleton<IXmlEncryptor>(services => new DpapiNGXmlEncryptor(protectionDescriptorRule, flags, services));
        }

        /// <summary>
        /// An <see cref="IXmlRepository"/> backed by a file system.
        /// </summary>
        public static ServiceDescriptor IXmlRepository_FileSystem(DirectoryInfo directory)
        {
            return ServiceDescriptor.Singleton<IXmlRepository>(services => new FileSystemXmlRepository(directory, services));
        }

        /// <summary>
        /// An <see cref="IXmlRepository"/> backed by volatile in-process memory.
        /// </summary>
        public static ServiceDescriptor IXmlRepository_InMemory()
        {
            return ServiceDescriptor.Singleton<IXmlRepository>(services => new EphemeralXmlRepository(services));
        }

        /// <summary>
        /// An <see cref="IXmlRepository"/> backed by the Windows registry.
        /// </summary>
        public static ServiceDescriptor IXmlRepository_Registry(RegistryKey registryKey)
        {
            return ServiceDescriptor.Singleton<IXmlRepository>(services => new RegistryXmlRepository(registryKey, services));
        }
    }
}
