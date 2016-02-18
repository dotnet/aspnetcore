// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// A type which allows reading policy from the system registry.
    /// </summary>
    internal sealed class RegistryPolicyResolver
    {
        private readonly RegistryKey _policyRegKey;

        internal RegistryPolicyResolver(RegistryKey policyRegKey)
        {
            _policyRegKey = policyRegKey;
        }

        // populates an options object from values stored in the registry
        private static void PopulateOptions(object options, RegistryKey key)
        {
            foreach (PropertyInfo propInfo in options.GetType().GetProperties())
            {
                if (propInfo.IsDefined(typeof(ApplyPolicyAttribute)))
                {
                    object valueFromRegistry = key.GetValue(propInfo.Name);
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
            List<string> sinks = new List<string>();

            // The format of this key is "type1; type2; ...".
            // We call Type.GetType to perform an eager check that the type exists.
            string sinksFromRegistry = (string)key.GetValue("KeyEscrowSinks");
            if (sinksFromRegistry != null)
            {
                foreach (string sinkFromRegistry in sinksFromRegistry.Split(';'))
                {
                    string candidate = sinkFromRegistry.Trim();
                    if (!String.IsNullOrEmpty(candidate))
                    {
                        typeof(IKeyEscrowSink).AssertIsAssignableFrom(Type.GetType(candidate, throwOnError: true));
                        sinks.Add(candidate);
                    }
                }
            }

            return sinks;
        }

        /// <summary>
        /// Returns an array of <see cref="ServiceDescriptor"/>s from the default registry location.
        /// </summary>
        public static ServiceDescriptor[] ResolveDefaultPolicy()
        {
            RegistryKey subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DotNetPackages\Microsoft.AspNetCore.DataProtection");
            if (subKey != null)
            {
                using (subKey)
                {
                    return new RegistryPolicyResolver(subKey).ResolvePolicy();
                }
            }
            else
            {
                return new ServiceDescriptor[0];
            }
        }

        internal ServiceDescriptor[] ResolvePolicy()
        {
            return ResolvePolicyCore().ToArray(); // fully evaluate enumeration while the reg key is open
        }

        private IEnumerable<ServiceDescriptor> ResolvePolicyCore()
        {
            // Read the encryption options type: CNG-CBC, CNG-GCM, Managed
            IInternalAuthenticatedEncryptionOptions options = null;
            string encryptionType = (string)_policyRegKey.GetValue("EncryptionType");
            if (String.Equals(encryptionType, "CNG-CBC", StringComparison.OrdinalIgnoreCase))
            {
                options = new CngCbcAuthenticatedEncryptionOptions();
            }
            else if (String.Equals(encryptionType, "CNG-GCM", StringComparison.OrdinalIgnoreCase))
            {
                options = new CngGcmAuthenticatedEncryptionOptions();
            }
            else if (String.Equals(encryptionType, "Managed", StringComparison.OrdinalIgnoreCase))
            {
                options = new ManagedAuthenticatedEncryptionOptions();
            }
            else if (!String.IsNullOrEmpty(encryptionType))
            {
                throw CryptoUtil.Fail("Unrecognized EncryptionType: " + encryptionType);
            }
            if (options != null)
            {
                PopulateOptions(options, _policyRegKey);
                yield return DataProtectionServiceDescriptors.IAuthenticatedEncryptorConfiguration_FromOptions(options);
            }

            // Read ancillary data

            int? defaultKeyLifetime = (int?)_policyRegKey.GetValue("DefaultKeyLifetime");
            if (defaultKeyLifetime.HasValue)
            {
                yield return DataProtectionServiceDescriptors.ConfigureOptions_DefaultKeyLifetime(defaultKeyLifetime.Value);
            }

            var keyEscrowSinks = ReadKeyEscrowSinks(_policyRegKey);
            foreach (var keyEscrowSink in keyEscrowSinks)
            {
                yield return DataProtectionServiceDescriptors.IKeyEscrowSink_FromTypeName(keyEscrowSink);
            }
        }
    }
}
