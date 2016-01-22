// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNet.DataProtection.KeyManagement;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNet.DataProtection
{
    public class RegistryPolicyResolverTests
    {
        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_NoEntries_ResultsInNoPolicies()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["unused"] = 42
            });

            Assert.Empty(serviceCollection);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_KeyEscrowSinks()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["KeyEscrowSinks"] = String.Join(" ;; ; ", new Type[] { typeof(MyKeyEscrowSink1), typeof(MyKeyEscrowSink2) }.Select(t => t.AssemblyQualifiedName))
            });

            var services = serviceCollection.BuildServiceProvider();
            var actualKeyEscrowSinks = services.GetService<IEnumerable<IKeyEscrowSink>>().ToArray();
            Assert.Equal(2, actualKeyEscrowSinks.Length);
            Assert.IsType(typeof(MyKeyEscrowSink1), actualKeyEscrowSinks[0]);
            Assert.IsType(typeof(MyKeyEscrowSink2), actualKeyEscrowSinks[1]);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_DefaultKeyLifetime()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["DefaultKeyLifetime"] = 1024 // days
            });

            var services = serviceCollection.BuildServiceProvider();
            var keyManagementOptions = services.GetService<IOptions<KeyManagementOptions>>();
            Assert.Equal(TimeSpan.FromDays(1024), keyManagementOptions.Value.NewKeyLifetime);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_CngCbcEncryption_WithoutExplicitSettings()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["EncryptionType"] = "cng-cbc"
            });

            var services = serviceCollection.BuildServiceProvider();
            var expectedConfiguration = new CngCbcAuthenticatedEncryptorConfiguration(new CngCbcAuthenticatedEncryptionOptions());
            var actualConfiguration = (CngCbcAuthenticatedEncryptorConfiguration)services.GetService<IAuthenticatedEncryptorConfiguration>();

            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithm, actualConfiguration.Options.EncryptionAlgorithm);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmKeySize, actualConfiguration.Options.EncryptionAlgorithmKeySize);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmProvider, actualConfiguration.Options.EncryptionAlgorithmProvider);
            Assert.Equal(expectedConfiguration.Options.HashAlgorithm, actualConfiguration.Options.HashAlgorithm);
            Assert.Equal(expectedConfiguration.Options.HashAlgorithmProvider, actualConfiguration.Options.HashAlgorithmProvider);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_CngCbcEncryption_WithExplicitSettings()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["EncryptionType"] = "cng-cbc",
                ["EncryptionAlgorithm"] = "enc-alg",
                ["EncryptionAlgorithmKeySize"] = 2048,
                ["EncryptionAlgorithmProvider"] = "my-enc-alg-provider",
                ["HashAlgorithm"] = "hash-alg",
                ["HashAlgorithmProvider"] = "my-hash-alg-provider"
            });

            var services = serviceCollection.BuildServiceProvider();
            var expectedConfiguration = new CngCbcAuthenticatedEncryptorConfiguration(new CngCbcAuthenticatedEncryptionOptions()
            {
                EncryptionAlgorithm = "enc-alg",
                EncryptionAlgorithmKeySize = 2048,
                EncryptionAlgorithmProvider = "my-enc-alg-provider",
                HashAlgorithm = "hash-alg",
                HashAlgorithmProvider = "my-hash-alg-provider"
            });
            var actualConfiguration = (CngCbcAuthenticatedEncryptorConfiguration)services.GetService<IAuthenticatedEncryptorConfiguration>();

            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithm, actualConfiguration.Options.EncryptionAlgorithm);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmKeySize, actualConfiguration.Options.EncryptionAlgorithmKeySize);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmProvider, actualConfiguration.Options.EncryptionAlgorithmProvider);
            Assert.Equal(expectedConfiguration.Options.HashAlgorithm, actualConfiguration.Options.HashAlgorithm);
            Assert.Equal(expectedConfiguration.Options.HashAlgorithmProvider, actualConfiguration.Options.HashAlgorithmProvider);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_CngGcmEncryption_WithoutExplicitSettings()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["EncryptionType"] = "cng-gcm"
            });

            var services = serviceCollection.BuildServiceProvider();
            var expectedConfiguration = new CngGcmAuthenticatedEncryptorConfiguration(new CngGcmAuthenticatedEncryptionOptions());
            var actualConfiguration = (CngGcmAuthenticatedEncryptorConfiguration)services.GetService<IAuthenticatedEncryptorConfiguration>();

            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithm, actualConfiguration.Options.EncryptionAlgorithm);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmKeySize, actualConfiguration.Options.EncryptionAlgorithmKeySize);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmProvider, actualConfiguration.Options.EncryptionAlgorithmProvider);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_CngGcmEncryption_WithExplicitSettings()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["EncryptionType"] = "cng-gcm",
                ["EncryptionAlgorithm"] = "enc-alg",
                ["EncryptionAlgorithmKeySize"] = 2048,
                ["EncryptionAlgorithmProvider"] = "my-enc-alg-provider"
            });

            var services = serviceCollection.BuildServiceProvider();
            var expectedConfiguration = new CngGcmAuthenticatedEncryptorConfiguration(new CngGcmAuthenticatedEncryptionOptions()
            {
                EncryptionAlgorithm = "enc-alg",
                EncryptionAlgorithmKeySize = 2048,
                EncryptionAlgorithmProvider = "my-enc-alg-provider"
            });
            var actualConfiguration = (CngGcmAuthenticatedEncryptorConfiguration)services.GetService<IAuthenticatedEncryptorConfiguration>();

            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithm, actualConfiguration.Options.EncryptionAlgorithm);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmKeySize, actualConfiguration.Options.EncryptionAlgorithmKeySize);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmProvider, actualConfiguration.Options.EncryptionAlgorithmProvider);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_ManagedEncryption_WithoutExplicitSettings()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["EncryptionType"] = "managed"
            });

            var services = serviceCollection.BuildServiceProvider();
            var expectedConfiguration = new ManagedAuthenticatedEncryptorConfiguration(new ManagedAuthenticatedEncryptionOptions());
            var actualConfiguration = (ManagedAuthenticatedEncryptorConfiguration)services.GetService<IAuthenticatedEncryptorConfiguration>();

            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmType, actualConfiguration.Options.EncryptionAlgorithmType);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmKeySize, actualConfiguration.Options.EncryptionAlgorithmKeySize);
            Assert.Equal(expectedConfiguration.Options.ValidationAlgorithmType, actualConfiguration.Options.ValidationAlgorithmType);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void ResolvePolicy_ManagedEncryption_WithExplicitSettings()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            RunTestWithRegValues(serviceCollection, new Dictionary<string, object>()
            {
                ["EncryptionType"] = "managed",
                ["EncryptionAlgorithmType"] = typeof(TripleDES).AssemblyQualifiedName,
                ["EncryptionAlgorithmKeySize"] = 2048,
                ["ValidationAlgorithmType"] = typeof(HMACSHA1).AssemblyQualifiedName
            });

            var services = serviceCollection.BuildServiceProvider();
            var expectedConfiguration = new ManagedAuthenticatedEncryptorConfiguration(new ManagedAuthenticatedEncryptionOptions()
            {
                EncryptionAlgorithmType = typeof(TripleDES),
                EncryptionAlgorithmKeySize = 2048,
                ValidationAlgorithmType = typeof(HMACSHA1)
            });
            var actualConfiguration = (ManagedAuthenticatedEncryptorConfiguration)services.GetService<IAuthenticatedEncryptorConfiguration>();

            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmType, actualConfiguration.Options.EncryptionAlgorithmType);
            Assert.Equal(expectedConfiguration.Options.EncryptionAlgorithmKeySize, actualConfiguration.Options.EncryptionAlgorithmKeySize);
            Assert.Equal(expectedConfiguration.Options.ValidationAlgorithmType, actualConfiguration.Options.ValidationAlgorithmType);
        }

        private static void RunTestWithRegValues(IServiceCollection services, Dictionary<string, object> regValues)
        {
            WithUniqueTempRegKey(registryKey =>
            {
                foreach (var entry in regValues)
                {
                    registryKey.SetValue(entry.Key, entry.Value);
                }

                var policyResolver = new RegistryPolicyResolver(registryKey);
                services.Add(policyResolver.ResolvePolicy());
            });
        }

        /// <summary>
        /// Runs a test and cleans up the registry key afterward.
        /// </summary>
        private static void WithUniqueTempRegKey(Action<RegistryKey> testCode)
        {
            string uniqueName = Guid.NewGuid().ToString();
            var uniqueSubkey = LazyHkcuTempKey.Value.CreateSubKey(uniqueName);
            try
            {
                testCode(uniqueSubkey);
            }
            finally
            {
                // clean up when test is done
                LazyHkcuTempKey.Value.DeleteSubKeyTree(uniqueName, throwOnMissingSubKey: false);
            }
        }

        private static readonly Lazy<RegistryKey> LazyHkcuTempKey = new Lazy<RegistryKey>(() =>
        {
            try
            {
                return Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\ASP.NET\temp");
            }
            catch
            {
                // swallow all failures
                return null;
            }
        });

        private class ConditionalRunTestOnlyIfHkcuRegistryAvailable : Attribute, ITestCondition
        {
            public bool IsMet => (PlatformServices.Default.Runtime.OperatingSystem == "Windows" && LazyHkcuTempKey.Value != null);

            public string SkipReason { get; } = "HKCU registry couldn't be opened.";
        }

        private class MyKeyEscrowSink1 : IKeyEscrowSink
        {
            public void Store(Guid keyId, XElement element)
            {
                throw new NotImplementedException();
            }
        }

        private class MyKeyEscrowSink2 : IKeyEscrowSink
        {
            public void Store(Guid keyId, XElement element)
            {
                throw new NotImplementedException();
            }
        }
    }
}
