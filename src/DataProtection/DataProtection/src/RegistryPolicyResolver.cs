// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// A type which allows reading policy from the system registry.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class RegistryPolicyResolver : IRegistryPolicyResolver
{
    private readonly Func<RegistryKey?> _getPolicyRegKey;
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

    private static List<string> ReadKeyEscrowSinks(RegistryKey key)
    {
        var sinks = new List<string>();

        // The format of this key is "type1; type2; ...".
        // We call Type.GetType to perform an eager check that the type exists.
        var sinksFromRegistry = (string?)key.GetValue("KeyEscrowSinks");
        if (sinksFromRegistry != null)
        {
            foreach (string sinkFromRegistry in sinksFromRegistry.Split(';'))
            {
                var candidate = sinkFromRegistry.Trim();
                if (!string.IsNullOrEmpty(candidate))
                {
                    typeof(IKeyEscrowSink).AssertIsAssignableFrom(TypeExtensions.GetTypeWithTrimFriendlyErrorMessage(candidate));
                    sinks.Add(candidate);
                }
            }
        }

        return sinks;
    }

    public RegistryPolicy? ResolvePolicy()
    {
        using (var registryKey = _getPolicyRegKey())
        {
            return ResolvePolicyCore(registryKey); // fully evaluate enumeration while the reg key is open
        }
    }

    private RegistryPolicy? ResolvePolicyCore(RegistryKey? policyRegKey)
    {
        if (policyRegKey == null)
        {
            return null;
        }

        // Read the encryption options type: CNG-CBC, CNG-GCM, Managed
        AlgorithmConfiguration? configuration = null;

        var encryptionType = (string?)policyRegKey.GetValue("EncryptionType");
        if (string.Equals(encryptionType, "CNG-CBC", StringComparison.OrdinalIgnoreCase))
        {
            configuration = GetCngCbcAuthenticatedEncryptorConfiguration(policyRegKey);
        }
        else if (string.Equals(encryptionType, "CNG-GCM", StringComparison.OrdinalIgnoreCase))
        {
            configuration = GetCngGcmAuthenticatedEncryptorConfiguration(policyRegKey);
        }
        else if (string.Equals(encryptionType, "Managed", StringComparison.OrdinalIgnoreCase))
        {
            configuration = GetManagedAuthenticatedEncryptorConfiguration(policyRegKey);
        }
        else if (!string.IsNullOrEmpty(encryptionType))
        {
            throw CryptoUtil.Fail("Unrecognized EncryptionType: " + encryptionType);
        }

        // Read ancillary data

        var defaultKeyLifetime = (int?)policyRegKey.GetValue("DefaultKeyLifetime");
        var escrowSinks = ReadKeyEscrowSinks(policyRegKey);
        var keyEscrowSinks = escrowSinks.Count is 0 ?
            Array.Empty<IKeyEscrowSink>() :
            new IKeyEscrowSink[escrowSinks.Count];
        for (var i = 0; i < keyEscrowSinks.Length; i++)
        {
            keyEscrowSinks[i] = _activator.CreateInstance<IKeyEscrowSink>(escrowSinks[i]);
        }

        return new RegistryPolicy(configuration, keyEscrowSinks, defaultKeyLifetime);
    }

    private static CngCbcAuthenticatedEncryptorConfiguration GetCngCbcAuthenticatedEncryptorConfiguration(RegistryKey key)
    {
        var options = new CngCbcAuthenticatedEncryptorConfiguration();
        var valueFromRegistry = key.GetValue(nameof(CngCbcAuthenticatedEncryptorConfiguration.EncryptionAlgorithm));
        if (valueFromRegistry != null)
        {
            options.EncryptionAlgorithm = Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture)!;
        }

        valueFromRegistry = key.GetValue(nameof(CngCbcAuthenticatedEncryptorConfiguration.EncryptionAlgorithmProvider));
        if (valueFromRegistry != null)
        {
            options.EncryptionAlgorithmProvider = Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture)!;
        }

        valueFromRegistry = key.GetValue(nameof(CngCbcAuthenticatedEncryptorConfiguration.EncryptionAlgorithmKeySize));
        if (valueFromRegistry != null)
        {
            options.EncryptionAlgorithmKeySize = Convert.ToInt32(valueFromRegistry, CultureInfo.InvariantCulture);
        }

        valueFromRegistry = key.GetValue(nameof(CngCbcAuthenticatedEncryptorConfiguration.HashAlgorithm));
        if (valueFromRegistry != null)
        {
            options.HashAlgorithm = Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture)!;
        }

        valueFromRegistry = key.GetValue(nameof(CngCbcAuthenticatedEncryptorConfiguration.HashAlgorithmProvider));
        if (valueFromRegistry != null)
        {
            options.HashAlgorithmProvider = Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture);
        }

        return options;
    }

    private static CngGcmAuthenticatedEncryptorConfiguration GetCngGcmAuthenticatedEncryptorConfiguration(RegistryKey key)
    {
        var options = new CngGcmAuthenticatedEncryptorConfiguration();
        var valueFromRegistry = key.GetValue(nameof(CngGcmAuthenticatedEncryptorConfiguration.EncryptionAlgorithm));
        if (valueFromRegistry != null)
        {
            options.EncryptionAlgorithm = Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture)!;
        }

        valueFromRegistry = key.GetValue(nameof(CngGcmAuthenticatedEncryptorConfiguration.EncryptionAlgorithmProvider));
        if (valueFromRegistry != null)
        {
            options.EncryptionAlgorithmProvider = Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture)!;
        }

        valueFromRegistry = key.GetValue(nameof(CngGcmAuthenticatedEncryptorConfiguration.EncryptionAlgorithmKeySize));
        if (valueFromRegistry != null)
        {
            options.EncryptionAlgorithmKeySize = Convert.ToInt32(valueFromRegistry, CultureInfo.InvariantCulture);
        }

        return options;
    }

    private static ManagedAuthenticatedEncryptorConfiguration GetManagedAuthenticatedEncryptorConfiguration(RegistryKey key)
    {
        var options = new ManagedAuthenticatedEncryptorConfiguration();
        var valueFromRegistry = key.GetValue(nameof(ManagedAuthenticatedEncryptorConfiguration.EncryptionAlgorithmType));
        if (valueFromRegistry != null)
        {
            options.EncryptionAlgorithmType = ManagedAlgorithmHelpers.FriendlyNameToType(Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture)!);
        }

        valueFromRegistry = key.GetValue(nameof(ManagedAuthenticatedEncryptorConfiguration.EncryptionAlgorithmKeySize));
        if (valueFromRegistry != null)
        {
            options.EncryptionAlgorithmKeySize = Convert.ToInt32(valueFromRegistry, CultureInfo.InvariantCulture);
        }

        valueFromRegistry = key.GetValue(nameof(ManagedAuthenticatedEncryptorConfiguration.ValidationAlgorithmType));
        if (valueFromRegistry != null)
        {
            options.ValidationAlgorithmType = ManagedAlgorithmHelpers.FriendlyNameToType(Convert.ToString(valueFromRegistry, CultureInfo.InvariantCulture)!);
        }

        return options;
    }
}
