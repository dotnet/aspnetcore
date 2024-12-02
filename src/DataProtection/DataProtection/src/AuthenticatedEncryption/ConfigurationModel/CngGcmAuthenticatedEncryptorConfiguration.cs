// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// Represents a configured authenticated encryption mechanism which uses
/// Windows CNG algorithms in GCM encryption + authentication modes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CngGcmAuthenticatedEncryptorConfiguration : AlgorithmConfiguration, IInternalAlgorithmConfiguration
{
    /// <summary>
    /// The name of the algorithm to use for symmetric encryption.
    /// This property corresponds to the 'pszAlgId' parameter of BCryptOpenAlgorithmProvider.
    /// This property is required to have a value.
    /// </summary>
    /// <remarks>
    /// The algorithm must support GCM-style encryption and must have a block size exactly
    /// 128 bits.
    /// The default value is 'AES'.
    /// </remarks>
    [ApplyPolicy]
    public string EncryptionAlgorithm { get; set; } = Constants.BCRYPT_AES_ALGORITHM;

    /// <summary>
    /// The name of the provider which contains the implementation of the symmetric encryption algorithm.
    /// This property corresponds to the 'pszImplementation' parameter of BCryptOpenAlgorithmProvider.
    /// This property is optional.
    /// </summary>
    /// <remarks>
    /// The default value is null.
    /// </remarks>
    [ApplyPolicy]
    public string? EncryptionAlgorithmProvider { get; set; } = null;

    /// <summary>
    /// The length (in bits) of the key that will be used for symmetric encryption.
    /// This property is required to have a value.
    /// </summary>
    /// <remarks>
    /// The key length must be 128 bits or greater.
    /// The default value is 256.
    /// </remarks>
    [ApplyPolicy]
    public int EncryptionAlgorithmKeySize { get; set; } = 256;

    /// <inheritdoc />
    public override IAuthenticatedEncryptorDescriptor CreateNewDescriptor()
    {
        var internalConfiguration = (IInternalAlgorithmConfiguration)this;
        return internalConfiguration.CreateDescriptorFromSecret(Secret.Random(KDK_SIZE_IN_BYTES));
    }

    IAuthenticatedEncryptorDescriptor IInternalAlgorithmConfiguration.CreateDescriptorFromSecret(ISecret secret)
    {
        return new CngGcmAuthenticatedEncryptorDescriptor(this, secret);
    }

    /// <summary>
    /// Validates that this <see cref="CngGcmAuthenticatedEncryptorConfiguration"/> is well-formed, i.e.,
    /// that the specified algorithm actually exists and can be instantiated properly.
    /// An exception will be thrown if validation fails.
    /// </summary>
    void IInternalAlgorithmConfiguration.Validate()
    {
        var factory = new CngGcmAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);
        // Run a sample payload through an encrypt -> decrypt operation to make sure data round-trips properly.
        using var secret = Secret.Random(512 / 8);
        using var encryptor = factory.CreateAuthenticatedEncryptorInstance(secret, this);
        encryptor.PerformSelfTest();
    }
}
