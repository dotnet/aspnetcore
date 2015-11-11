// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Cryptography.Cng;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNet.DataProtection.Cng;
using Microsoft.AspNet.DataProtection.KeyManagement;
using Microsoft.AspNet.DataProtection.KeyManagement.Internal;
using Microsoft.AspNet.DataProtection.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides access to default Data Protection <see cref="ServiceDescriptor"/> instances.
    /// </summary>
    public static class DataProtectionServices
    {
        /// <summary>
        /// Returns a collection of default <see cref="ServiceDescriptor"/> instances that can be
        /// used to bootstrap the Data Protection system.
        /// </summary>
        public static IEnumerable<ServiceDescriptor> GetDefaultServices()
        {
            // The default key services are a strange beast. We don't want to return
            // IXmlEncryptor and IXmlRepository as-is because they almost always have to be
            // set as a matched pair. Instead, our built-in key manager will use a meta-service
            // which represents the default pairing (logic based on hosting environment as
            // demonstrated below), and if the developer explicitly specifies one or the other
            // we'll not use the fallback at all.
            yield return ServiceDescriptor.Singleton<IDefaultKeyServices>(services =>
            {
                ILogger log = services.GetLogger(typeof(DataProtectionServices));

                ServiceDescriptor keyEncryptorDescriptor = null;
                ServiceDescriptor keyRepositoryDescriptor = null;

                // If we're running in Azure Web Sites, the key repository goes in the %HOME% directory.
                var azureWebSitesKeysFolder = FileSystemXmlRepository.GetKeyStorageDirectoryForAzureWebSites();
                if (azureWebSitesKeysFolder != null)
                {
                    log?.UsingAzureAsKeyRepository(azureWebSitesKeysFolder.FullName);

                    // Cloud DPAPI isn't yet available, so we don't encrypt keys at rest.
                    // This isn't all that different than what Azure Web Sites does today, and we can always add this later.
                    keyRepositoryDescriptor = DataProtectionServiceDescriptors.IXmlRepository_FileSystem(azureWebSitesKeysFolder);
                }
                else
                {
                    // If the user profile is available, store keys in the user profile directory.
                    var localAppDataKeysFolder = FileSystemXmlRepository.DefaultKeyStorageDirectory;
                    if (localAppDataKeysFolder != null)
                    {
                        if (OSVersionUtil.IsWindows())
                        {
                            // If the user profile is available, we can protect using DPAPI.
                            // Probe to see if protecting to local user is available, and use it as the default if so.
                            keyEncryptorDescriptor = DataProtectionServiceDescriptors.IXmlEncryptor_Dpapi(protectToMachine: !DpapiSecretSerializerHelper.CanProtectToCurrentUserAccount());
                        }
                        keyRepositoryDescriptor = DataProtectionServiceDescriptors.IXmlRepository_FileSystem(localAppDataKeysFolder);

                        if (keyEncryptorDescriptor != null)
                        {
                            log?.UsingProfileAsKeyRepositoryWithDPAPI(localAppDataKeysFolder.FullName);
                        }
                        else
                        {
                            log?.UsingProfileAsKeyRepository(localAppDataKeysFolder.FullName);
                        }
                    }
                    else
                    {
                        // Use profile isn't available - can we use the HKLM registry?
                        RegistryKey regKeyStorageKey = null;
                        if (OSVersionUtil.IsWindows())
                        {
                            regKeyStorageKey = RegistryXmlRepository.DefaultRegistryKey;
                        }
                        if (regKeyStorageKey != null)
                        {
                            // If the user profile isn't available, we can protect using DPAPI (to machine).
                            keyEncryptorDescriptor = DataProtectionServiceDescriptors.IXmlEncryptor_Dpapi(protectToMachine: true);
                            keyRepositoryDescriptor = DataProtectionServiceDescriptors.IXmlRepository_Registry(regKeyStorageKey);

                            log?.UsingRegistryAsKeyRepositoryWithDPAPI(regKeyStorageKey.Name);
                        }
                        else
                        {
                            // Final fallback - use an ephemeral repository since we don't know where else to go.
                            // This can only be used for development scenarios.
                            keyRepositoryDescriptor = DataProtectionServiceDescriptors.IXmlRepository_InMemory();

                            log?.UsingEphemeralKeyRepository();
                        }
                    }
                }

                return new DefaultKeyServices(
                    services: services,
                    keyEncryptorDescriptor: keyEncryptorDescriptor,
                    keyRepositoryDescriptor: keyRepositoryDescriptor);
            });

            // Provide root key management and data protection services
            yield return DataProtectionServiceDescriptors.IKeyManager_Default();
            yield return DataProtectionServiceDescriptors.IDataProtectionProvider_Default();

            // Provide services required for XML encryption
#if !DOTNET5_4 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml
            yield return DataProtectionServiceDescriptors.ICertificateResolver_Default();
#endif

            // Hook up the logic which allows populating default options
            yield return DataProtectionServiceDescriptors.ConfigureOptions_DataProtectionOptions();

            // Read and apply policy from the registry, overriding any other defaults.
            bool encryptorConfigurationReadFromRegistry = false;
            if (OSVersionUtil.IsWindows())
            {
                foreach (var descriptor in RegistryPolicyResolver.ResolveDefaultPolicy())
                {
                    yield return descriptor;
                    if (descriptor.ServiceType == typeof(IAuthenticatedEncryptorConfiguration))
                    {
                        encryptorConfigurationReadFromRegistry = true;
                    }
                }
            }

            // Finally, provide a fallback encryptor configuration if one wasn't already specified.
            if (!encryptorConfigurationReadFromRegistry)
            {
                yield return DataProtectionServiceDescriptors.IAuthenticatedEncryptorConfiguration_Default();
            }
        }
    }
}
