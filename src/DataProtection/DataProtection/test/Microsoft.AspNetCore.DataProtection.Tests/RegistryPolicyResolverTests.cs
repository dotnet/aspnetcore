// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.DataProtection;

public class RegistryPolicyResolverTests
{
    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_NoEntries_ResultsInNoPolicies()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>();

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        Assert.Null(context.EncryptorConfiguration);
        Assert.Null(context.DefaultKeyLifetime);
        Assert.Empty(context.KeyEscrowSinks);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_KeyEscrowSinks()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>()
        {
            ["KeyEscrowSinks"] = String.Join(" ;; ; ", new Type[] { typeof(MyKeyEscrowSink1), typeof(MyKeyEscrowSink2) }.Select(t => t.AssemblyQualifiedName))
        };

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        var actualKeyEscrowSinks = context.KeyEscrowSinks.ToArray();
        Assert.Equal(2, actualKeyEscrowSinks.Length);
        Assert.IsType<MyKeyEscrowSink1>(actualKeyEscrowSinks[0]);
        Assert.IsType<MyKeyEscrowSink2>(actualKeyEscrowSinks[1]);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_MissingKeyEscrowSinks()
    {
        // Arrange
        var typeName = typeof(MyKeyEscrowSink1).AssemblyQualifiedName.Replace("MyKeyEscrowSink1", "MyKeyEscrowSinkDontExist");
        var registryEntries = new Dictionary<string, object>()
        {
            ["KeyEscrowSinks"] = typeName
        };

        // Act
        var ex = ExceptionAssert.Throws<InvalidOperationException>(() => RunTestWithRegValues(registryEntries));

        // Assert
        Assert.Equal($"Unable to load type '{typeName}'. If the app is published with trimming then this type may have been trimmed. Ensure the type's assembly is excluded from trimming.", ex.Message);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_DefaultKeyLifetime()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>()
        {
            ["DefaultKeyLifetime"] = 1024 // days
        };

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        Assert.Equal(1024, context.DefaultKeyLifetime);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_CngCbcEncryption_WithoutExplicitSettings()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>()
        {
            ["EncryptionType"] = "cng-cbc"
        };
        var expectedConfiguration = new CngCbcAuthenticatedEncryptorConfiguration();

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        var actualConfiguration = (CngCbcAuthenticatedEncryptorConfiguration)context.EncryptorConfiguration;

        Assert.Equal(expectedConfiguration.EncryptionAlgorithm, actualConfiguration.EncryptionAlgorithm);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmKeySize, actualConfiguration.EncryptionAlgorithmKeySize);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmProvider, actualConfiguration.EncryptionAlgorithmProvider);
        Assert.Equal(expectedConfiguration.HashAlgorithm, actualConfiguration.HashAlgorithm);
        Assert.Equal(expectedConfiguration.HashAlgorithmProvider, actualConfiguration.HashAlgorithmProvider);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_CngCbcEncryption_WithExplicitSettings()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>()
        {
            ["EncryptionType"] = "cng-cbc",
            ["EncryptionAlgorithm"] = "enc-alg",
            ["EncryptionAlgorithmKeySize"] = 2048,
            ["EncryptionAlgorithmProvider"] = "my-enc-alg-provider",
            ["HashAlgorithm"] = "hash-alg",
            ["HashAlgorithmProvider"] = "my-hash-alg-provider"
        };
        var expectedConfiguration = new CngCbcAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithm = "enc-alg",
            EncryptionAlgorithmKeySize = 2048,
            EncryptionAlgorithmProvider = "my-enc-alg-provider",
            HashAlgorithm = "hash-alg",
            HashAlgorithmProvider = "my-hash-alg-provider"
        };

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        var actualConfiguration = (CngCbcAuthenticatedEncryptorConfiguration)context.EncryptorConfiguration;

        Assert.Equal(expectedConfiguration.EncryptionAlgorithm, actualConfiguration.EncryptionAlgorithm);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmKeySize, actualConfiguration.EncryptionAlgorithmKeySize);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmProvider, actualConfiguration.EncryptionAlgorithmProvider);
        Assert.Equal(expectedConfiguration.HashAlgorithm, actualConfiguration.HashAlgorithm);
        Assert.Equal(expectedConfiguration.HashAlgorithmProvider, actualConfiguration.HashAlgorithmProvider);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_CngGcmEncryption_WithoutExplicitSettings()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>()
        {
            ["EncryptionType"] = "cng-gcm"
        };
        var expectedConfiguration = new CngGcmAuthenticatedEncryptorConfiguration();

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        var actualConfiguration = (CngGcmAuthenticatedEncryptorConfiguration)context.EncryptorConfiguration;

        Assert.Equal(expectedConfiguration.EncryptionAlgorithm, actualConfiguration.EncryptionAlgorithm);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmKeySize, actualConfiguration.EncryptionAlgorithmKeySize);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmProvider, actualConfiguration.EncryptionAlgorithmProvider);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_CngGcmEncryption_WithExplicitSettings()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>()
        {
            ["EncryptionType"] = "cng-gcm",
            ["EncryptionAlgorithm"] = "enc-alg",
            ["EncryptionAlgorithmKeySize"] = 2048,
            ["EncryptionAlgorithmProvider"] = "my-enc-alg-provider"
        };
        var expectedConfiguration = new CngGcmAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithm = "enc-alg",
            EncryptionAlgorithmKeySize = 2048,
            EncryptionAlgorithmProvider = "my-enc-alg-provider"
        };

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        var actualConfiguration = (CngGcmAuthenticatedEncryptorConfiguration)context.EncryptorConfiguration;

        Assert.Equal(expectedConfiguration.EncryptionAlgorithm, actualConfiguration.EncryptionAlgorithm);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmKeySize, actualConfiguration.EncryptionAlgorithmKeySize);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmProvider, actualConfiguration.EncryptionAlgorithmProvider);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_ManagedEncryption_WithoutExplicitSettings()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>()
        {
            ["EncryptionType"] = "managed"
        };
        var expectedConfiguration = new ManagedAuthenticatedEncryptorConfiguration();

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        var actualConfiguration = (ManagedAuthenticatedEncryptorConfiguration)context.EncryptorConfiguration;

        Assert.Equal(expectedConfiguration.EncryptionAlgorithmType, actualConfiguration.EncryptionAlgorithmType);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmKeySize, actualConfiguration.EncryptionAlgorithmKeySize);
        Assert.Equal(expectedConfiguration.ValidationAlgorithmType, actualConfiguration.ValidationAlgorithmType);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyIfHkcuRegistryAvailable]
    public void ResolvePolicy_ManagedEncryption_WithExplicitSettings()
    {
        // Arrange
        var registryEntries = new Dictionary<string, object>()
        {
            ["EncryptionType"] = "managed",
            ["EncryptionAlgorithmType"] = typeof(Aes).AssemblyQualifiedName,
            ["EncryptionAlgorithmKeySize"] = 2048,
            ["ValidationAlgorithmType"] = typeof(HMACSHA1).AssemblyQualifiedName
        };
        var expectedConfiguration = new ManagedAuthenticatedEncryptorConfiguration()
        {
            EncryptionAlgorithmType = typeof(Aes),
            EncryptionAlgorithmKeySize = 2048,
            ValidationAlgorithmType = typeof(HMACSHA1)
        };

        // Act
        var context = RunTestWithRegValues(registryEntries);

        // Assert
        var actualConfiguration = (ManagedAuthenticatedEncryptorConfiguration)context.EncryptorConfiguration;

        Assert.Equal(expectedConfiguration.EncryptionAlgorithmType, actualConfiguration.EncryptionAlgorithmType);
        Assert.Equal(expectedConfiguration.EncryptionAlgorithmKeySize, actualConfiguration.EncryptionAlgorithmKeySize);
        Assert.Equal(expectedConfiguration.ValidationAlgorithmType, actualConfiguration.ValidationAlgorithmType);
    }

    private static RegistryPolicy RunTestWithRegValues(Dictionary<string, object> regValues)
    {
        return WithUniqueTempRegKey(registryKey =>
        {
            foreach (var entry in regValues)
            {
                registryKey.SetValue(entry.Key, entry.Value);
            }

            var policyResolver = new RegistryPolicyResolver(
                registryKey,
                activator: SimpleActivator.DefaultWithoutServices);

            return policyResolver.ResolvePolicy();
        });
    }

    /// <summary>
    /// Runs a test and cleans up the registry key afterward.
    /// </summary>
    private static RegistryPolicy WithUniqueTempRegKey(Func<RegistryKey, RegistryPolicy> testCode)
    {
        string uniqueName = Guid.NewGuid().ToString();
        var uniqueSubkey = LazyHkcuTempKey.Value.CreateSubKey(uniqueName);
        try
        {
            return testCode(uniqueSubkey);
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
