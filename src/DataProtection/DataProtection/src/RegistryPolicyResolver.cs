// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// A type which allows reading policy from the system registry.
    /// </summary>
    internal sealed class RegistryPolicyResolver: IRegistryPolicyResolver
    {
        private readonly Func<RegistryKey> _getPolicyRegKey;
        private readonly IActivator _activator;

        public RegistryPolicyResolver(IActivator activator)
        {
            _getPolicyRegKey = () => Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DotNetPackages\Microsoft.AspNetCore.DataProtection");
            _activator = activator;
        }

        internal RegistryPolicyResolver(RegistryKey policyRegKey, IActivator activator)
        {
            _getPolicyRegKey = () => policyRegKey;
            _activator = activator;
        }

        // populates an options object from values stored in the registry
        private static void PopulateOptions(object options, RegistryKey key)
        {
            foreach (PropertyInfo propInfo in options.GetType().GetProperties())
            {
                if (propInfo.IsDefined(typeof(ApplyPolicyAttribute)))
                {
                    var valueFromRegistry = key.GetValue(propInfo.Name);
                    if (valueFromRegistry != null)
                    {
                        if (propInfo.PropertyType == typeof(string))
                        {
                            propInfo.SetValue(options, Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture));
                        }
                        else if (propInfo.PropertyType == typeof(int))
                        {
                            propInfo.SetValue(options, Convert.ToInt32(valueFromRegistry, CultureInfo.InvariantCulture));
                        }
                        else if (propInfo.PropertyType == typeof(Type))
                        {
                            propInfo.SetValue(options, Type.GetType(Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture), throwOnError: true));
                        }
                        else
                        {
                            throw CryptoUtil.Fail("Unexpected type on property: " + propInfo.Name);
                        }
                    }
                }
            }
        }

        private static List<string> ReadKeyEscrowSinks(RegistryKey key)
        {
            var sinks = new List<string>();

            // The format of this key is "type1; type2; ...".
            // We call Type.GetType to perform an eager check that the type exists.
            var sinksFromRegistry = (string)key.GetValue("KeyEscrowSinks");
            if (sinksFromRegistry != null)
            {
                foreach (string sinkFromRegistry in sinksFromRegistry.Split(';'))
                {
                    var candidate = sinkFromRegistry.Trim();
                    if (!String.IsNullOrEmpty(candidate))
                    {
                        typeof(IKeyEscrowSink).AssertIsAssignableFrom(Type.GetType(candidate, throwOnError: true));
                        sinks.Add(candidate);
                    }
                }
            }

            return sinks;
        }

        public RegistryPolicy ResolvePolicy()
        {
            using (var registryKey = _getPolicyRegKey())
            {
                return ResolvePolicyCore(registryKey); // fully evaluate enumeration while the reg key is open
            }
        }

        private RegistryPolicy ResolvePolicyCore(RegistryKey policyRegKey)
        {
            if (policyRegKey == null)
            {
                return null;
            }

            // Read the encryption options type: CNG-CBC, CNG-GCM, Managed
            AlgorithmConfiguration configuration = null;

            var encryptionType = (string)policyRegKey.GetValue("EncryptionType");
            if (String.Equals(encryptionType, "CNG-CBC", StringComparison.OrdinalIgnoreCase))
            {
                configuration = new CngCbcAuthenticatedEncryptorConfiguration();
            }
            else if (String.Equals(encryptionType, "CNG-GCM", StringComparison.OrdinalIgnoreCase))
            {
                configuration = new CngGcmAuthenticatedEncryptorConfiguration();
            }
            else if (String.Equals(encryptionType, "Managed", StringComparison.OrdinalIgnoreCase))
            {
                configuration = new ManagedAuthenticatedEncryptorConfiguration();
            }
            else if (!String.IsNullOrEmpty(encryptionType))
            {
                throw CryptoUtil.Fail("Unrecognized EncryptionType: " + encryptionType);
            }
            if (configuration != null)
            {
                PopulateOptions(configuration, policyRegKey);
            }

            // Read ancillary data

            var defaultKeyLifetime = (int?)policyRegKey.GetValue("DefaultKeyLifetime");

            var keyEscrowSinks = ReadKeyEscrowSinks(policyRegKey).Select(item => _activator.CreateInstance<IKeyEscrowSink>(item));

            return new RegistryPolicy(configuration, keyEscrowSinks, defaultKeyLifetime);
        }
    }
}
