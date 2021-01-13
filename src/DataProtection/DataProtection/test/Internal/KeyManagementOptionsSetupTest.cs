// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    public class KeyManagementOptionsSetupTest
    {
        [Fact]
        public void Configure_SetsExpectedValues()
        {
            // Arrange
            var setup = new KeyManagementOptionsSetup(NullLoggerFactory.Instance);
            var options = new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = null
            };

            // Act
            setup.Configure(options);

            // Assert
            Assert.Empty(options.KeyEscrowSinks);
            Assert.NotNull(options.AuthenticatedEncryptorConfiguration);
            Assert.IsType<AuthenticatedEncryptorConfiguration>(options.AuthenticatedEncryptorConfiguration);
            Assert.Collection(
                options.AuthenticatedEncryptorFactories,
                f => Assert.IsType<CngGcmAuthenticatedEncryptorFactory>(f),
                f => Assert.IsType<CngCbcAuthenticatedEncryptorFactory>(f),
                f => Assert.IsType<ManagedAuthenticatedEncryptorFactory>(f),
                f => Assert.IsType<AuthenticatedEncryptorFactory>(f));
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
        public void Configure_WithRegistryPolicyResolver_SetsValuesFromResolver()
        {
            // Arrange
            var registryEntries = new Dictionary<string, object>()
            {
                ["KeyEscrowSinks"] = String.Join(" ;; ; ", new Type[] { typeof(MyKeyEscrowSink1), typeof(MyKeyEscrowSink2) }.Select(t => t.AssemblyQualifiedName)),
                ["EncryptionType"] = "managed",
                ["DefaultKeyLifetime"] = 1024 // days
            };
            var options = new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = null
            };

            // Act
            RunTest(registryEntries, options);

            // Assert
            Assert.Collection(
                options.KeyEscrowSinks,
                k => Assert.IsType<MyKeyEscrowSink1>(k),
                k => Assert.IsType<MyKeyEscrowSink2>(k));
            Assert.Equal(TimeSpan.FromDays(1024), options.NewKeyLifetime);
            Assert.NotNull(options.AuthenticatedEncryptorConfiguration);
            Assert.IsType<ManagedAuthenticatedEncryptorConfiguration>(options.AuthenticatedEncryptorConfiguration);
            Assert.Collection(
                options.AuthenticatedEncryptorFactories,
                f => Assert.IsType<CngGcmAuthenticatedEncryptorFactory>(f),
                f => Assert.IsType<CngCbcAuthenticatedEncryptorFactory>(f),
                f => Assert.IsType<ManagedAuthenticatedEncryptorFactory>(f),
                f => Assert.IsType<AuthenticatedEncryptorFactory>(f));
        }

        private static void RunTest(Dictionary<string, object> regValues, KeyManagementOptions options)
        {
            WithUniqueTempRegKey(registryKey =>
            {
                foreach (var entry in regValues)
                {
                    registryKey.SetValue(entry.Key, entry.Value);
                }

                var policyResolver = new RegistryPolicyResolver(
                    registryKey,
                    activator: SimpleActivator.DefaultWithoutServices);

                var setup = new KeyManagementOptionsSetup(NullLoggerFactory.Instance, policyResolver);

                setup.Configure(options);
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
            public bool IsMet => (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && LazyHkcuTempKey.Value != null);

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
