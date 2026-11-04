// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

/// <summary>
/// An <see cref="IAuthenticatedEncryptorFactory"/> for <see cref="CngGcmAuthenticatedEncryptor"/>.
/// </summary>
public sealed class CngGcmAuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CngCbcAuthenticatedEncryptorFactory"/>.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public CngGcmAuthenticatedEncryptorFactory(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CngGcmAuthenticatedEncryptorFactory>();
    }

    /// <inheritdoc />
    public IAuthenticatedEncryptor? CreateEncryptorInstance(IKey key)
    {
        var descriptor = key.Descriptor as CngGcmAuthenticatedEncryptorDescriptor;
        if (descriptor == null)
        {
            return null;
        }

        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        return CreateAuthenticatedEncryptorInstance(descriptor.MasterKey, descriptor.Configuration);
    }

    [SupportedOSPlatform("windows")]
    [return: NotNullIfNotNull("configuration")]
    internal CngGcmAuthenticatedEncryptor? CreateAuthenticatedEncryptorInstance(
        ISecret secret,
        CngGcmAuthenticatedEncryptorConfiguration configuration)
    {
        if (configuration == null)
        {
            return null;
        }

        using var key = new Secret(secret);
        return new CngGcmAuthenticatedEncryptor(
            keyDerivationKey: key,
            symmetricAlgorithmHandle: GetSymmetricBlockCipherAlgorithmHandle(configuration),
            symmetricAlgorithmKeySizeInBytes: (uint)(configuration.EncryptionAlgorithmKeySize / 8));
    }

    [SupportedOSPlatform("windows")]
    private BCryptAlgorithmHandle GetSymmetricBlockCipherAlgorithmHandle(CngGcmAuthenticatedEncryptorConfiguration configuration)
    {
        // basic argument checking
        if (String.IsNullOrEmpty(configuration.EncryptionAlgorithm))
        {
            throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(EncryptionAlgorithm));
        }
        if (configuration.EncryptionAlgorithmKeySize < 0)
        {
            throw Error.Common_PropertyMustBeNonNegative(nameof(configuration.EncryptionAlgorithmKeySize));
        }

        BCryptAlgorithmHandle? algorithmHandle = null;

        _logger.OpeningCNGAlgorithmFromProviderWithChainingModeGCM(configuration.EncryptionAlgorithm, configuration.EncryptionAlgorithmProvider);
        // Special-case cached providers
        if (configuration.EncryptionAlgorithmProvider == null)
        {
            if (configuration.EncryptionAlgorithm == Constants.BCRYPT_AES_ALGORITHM) { algorithmHandle = CachedAlgorithmHandles.AES_GCM; }
        }

        // Look up the provider dynamically if we couldn't fetch a cached instance
        if (algorithmHandle == null)
        {
            algorithmHandle = BCryptAlgorithmHandle.OpenAlgorithmHandle(configuration.EncryptionAlgorithm, configuration.EncryptionAlgorithmProvider);
            algorithmHandle.SetChainingMode(Constants.BCRYPT_CHAIN_MODE_GCM);
        }

        // make sure we're using a block cipher with an appropriate key size & block size
        CryptoUtil.Assert(algorithmHandle.GetCipherBlockLength() == 128 / 8, "GCM requires a block cipher algorithm with a 128-bit block size.");
        AlgorithmAssert.IsAllowableSymmetricAlgorithmKeySize(checked((uint)configuration.EncryptionAlgorithmKeySize));

        // make sure the provided key length is valid
        algorithmHandle.GetSupportedKeyLengths().EnsureValidKeyLength((uint)configuration.EncryptionAlgorithmKeySize);

        // all good!
        return algorithmHandle;
    }
}
