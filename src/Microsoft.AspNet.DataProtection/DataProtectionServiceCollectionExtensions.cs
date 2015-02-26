// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNet.Cryptography.Cng;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.DataProtection.Dpapi;
using Microsoft.AspNet.DataProtection.KeyManagement;
using Microsoft.AspNet.DataProtection.Repositories;
using Microsoft.AspNet.DataProtection.XmlEncryption;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class DataProtectionServiceCollectionExtensions
    {
        public static IServiceCollection AddDataProtection(this IServiceCollection services, IConfiguration configuration = null)
        {
            services.AddOptions(configuration);
            var describe = new ServiceDescriber(configuration);
            services.TryAdd(OSVersionUtil.IsBCryptOnWin7OrLaterAvailable()
                ? GetDefaultServicesWindows(describe)
                : GetDefaultServicesNonWindows(describe));
            return services;
        }

        private static IEnumerable<IServiceDescriptor> GetDefaultServicesNonWindows(ServiceDescriber describe)
        {
            // If we're not running on Windows, we can't use CNG.

            // TODO: Replace this with something else. Mono's implementation of the
            // DPAPI routines don't provide authenticity.
            return new[]
            {
                describe.Instance<IDataProtectionProvider>(new DpapiDataProtectionProvider(DataProtectionScope.CurrentUser))
            };
        }

        private static IEnumerable<IServiceDescriptor> GetDefaultServicesWindows(ServiceDescriber describe)
        {
            List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();

            // Are we running in Azure Web Sites?
            DirectoryInfo azureWebSitesKeysFolder = TryGetKeysFolderForAzureWebSites();
            if (azureWebSitesKeysFolder != null)
            {
                // We'll use a null protector at the moment until the
                // cloud DPAPI service comes online.
                descriptors.AddRange(new[]
                {
                    describe.Singleton<IXmlEncryptor,NullXmlEncryptor>(),
                    describe.Instance<IXmlRepository>(new FileSystemXmlRepository(azureWebSitesKeysFolder))
                });
            }
            else
            {
                // Are we running with the user profile loaded?
                DirectoryInfo localAppDataKeysFolder = TryGetLocalAppDataKeysFolderForUser();
                if (localAppDataKeysFolder != null)
                {
                    descriptors.AddRange(new[]
                    {
                        describe.Instance<IXmlEncryptor>(new DpapiXmlEncryptor(protectToLocalMachine: false)),
                        describe.Instance<IXmlRepository>(new FileSystemXmlRepository(localAppDataKeysFolder))
                    });
                }
                else
                {
                    // If we've reached this point, we have no user profile loaded.

                    RegistryXmlRepository hklmRegXmlRepository = RegistryXmlRepository.GetDefaultRepositoryForHKLMRegistry();
                    if (hklmRegXmlRepository != null)
                    {
                        // Have WAS and IIS created an auto-gen key folder in the HKLM registry for us?
                        // If so, use it as the repository, and use DPAPI as the key protection mechanism.
                        // We use same-machine DPAPI since we already know no user profile is loaded.
                        descriptors.AddRange(new[]
                        {
                            describe.Instance<IXmlEncryptor>(new DpapiXmlEncryptor(protectToLocalMachine: true)),
                            describe.Instance<IXmlRepository>(hklmRegXmlRepository)
                        });
                    }
                    else
                    {
                        // Fall back to DPAPI for now
                        return new[] {
                            describe.Instance<IDataProtectionProvider>(new DpapiDataProtectionProvider(DataProtectionScope.LocalMachine))
                        };
                    }
                }
            }

            // We use CNG CBC + HMAC by default.
            descriptors.AddRange(new[]
            {
                describe.Singleton<IAuthenticatedEncryptorConfigurationFactory, CngCbcAuthenticatedEncryptorConfigurationFactory>(),
                describe.Singleton<ITypeActivator, TypeActivator>(),
                describe.Singleton<IKeyManager, XmlKeyManager>(),
                describe.Singleton<IDataProtectionProvider, DefaultDataProtectionProvider>()
            });

            return descriptors;
        }

        private static DirectoryInfo TryGetKeysFolderForAzureWebSites()
        {
            // There are two environment variables we care about.
            if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
            {
                return null;
            }

            string homeEnvVar = Environment.GetEnvironmentVariable("HOME");
            if (String.IsNullOrEmpty(homeEnvVar))
            {
                return null;
            }

            // TODO: Remove BETA moniker from below.
            string fullPathToKeys = Path.Combine(homeEnvVar, "ASP.NET", "keys-BETA6");
            return new DirectoryInfo(fullPathToKeys);
        }

        private static DirectoryInfo TryGetLocalAppDataKeysFolderForUser()
        {
#if !ASPNETCORE50
            // Environment.GetFolderPath returns null if the user profile isn't loaded.
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!String.IsNullOrEmpty(folderPath))
            {
                // TODO: Remove BETA moniker from below.
                return new DirectoryInfo(Path.Combine(folderPath, "ASP.NET", "keys-BETA6"));
            }
            else
            {
                return null;
            }
#else
            // On core CLR, we need to fall back to environment variables.
            string folderPath = Environment.GetEnvironmentVariable("LOCALAPPDATA")
                ?? Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "AppData", "Local");

            // TODO: Remove BETA moniker from below.
            DirectoryInfo retVal = new DirectoryInfo(Path.Combine(folderPath, "ASP.NET", "keys-BETA6"));
            try
            {
                retVal.Create(); // throws if we don't have access, e.g., user profile not loaded
                return retVal;
            } catch
            {
                return null;
            }
#endif
        }
    }
}
