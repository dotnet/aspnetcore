// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Managed;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

/// <summary>
/// An <see cref="IAuthenticatedEncryptorFactory"/> for <see cref="ManagedAuthenticatedEncryptor"/>.
/// </summary>
public sealed class ManagedAuthenticatedEncryptorFactory : IAuthenticatedEncryptorFactory
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ManagedAuthenticatedEncryptorFactory"/>.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public ManagedAuthenticatedEncryptorFactory(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ManagedAuthenticatedEncryptorFactory>();
    }

    /// <inheritdoc />
    public IAuthenticatedEncryptor? CreateEncryptorInstance(IKey key)
    {
        if (key.Descriptor is not ManagedAuthenticatedEncryptorDescriptor descriptor)
        {
            return null;
        }

        return CreateAuthenticatedEncryptorInstance(descriptor.MasterKey, descriptor.Configuration);
    }

    [return: NotNullIfNotNull("configuration")]
    internal ManagedAuthenticatedEncryptor? CreateAuthenticatedEncryptorInstance(
        ISecret secret,
        ManagedAuthenticatedEncryptorConfiguration? configuration)
    {
        if (configuration == null)
        {
            return null;
        }

        return new ManagedAuthenticatedEncryptor(
            keyDerivationKey: new Secret(secret),
            symmetricAlgorithmFactory: GetSymmetricBlockCipherAlgorithmFactory(configuration),
            symmetricAlgorithmKeySizeInBytes: configuration.EncryptionAlgorithmKeySize / 8,
            validationAlgorithmFactory: GetKeyedHashAlgorithmFactory(configuration));
    }

    private Func<KeyedHashAlgorithm> GetKeyedHashAlgorithmFactory(ManagedAuthenticatedEncryptorConfiguration configuration)
    {
        // basic argument checking
        if (configuration.ValidationAlgorithmType == null)
        {
            throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(configuration.ValidationAlgorithmType));
        }

        typeof(KeyedHashAlgorithm).AssertIsAssignableFrom(configuration.ValidationAlgorithmType);
        _logger.UsingManagedKeyedHashAlgorithm(configuration.ValidationAlgorithmType.FullName!);
        if (configuration.ValidationAlgorithmType == typeof(HMACSHA256))
        {
            return () => new HMACSHA256();
        }
        else if (configuration.ValidationAlgorithmType == typeof(HMACSHA512))
        {
            return () => new HMACSHA512();
        }
        else
        {
            return AlgorithmActivator.CreateFactory<KeyedHashAlgorithm>(configuration.ValidationAlgorithmType);
        }
    }

    private Func<SymmetricAlgorithm> GetSymmetricBlockCipherAlgorithmFactory(ManagedAuthenticatedEncryptorConfiguration configuration)
    {
        // basic argument checking
        if (configuration.EncryptionAlgorithmType == null)
        {
            throw Error.Common_PropertyCannotBeNullOrEmpty(nameof(configuration.EncryptionAlgorithmType));
        }
        typeof(SymmetricAlgorithm).AssertIsAssignableFrom(configuration.EncryptionAlgorithmType);
        if (configuration.EncryptionAlgorithmKeySize < 0)
        {
            throw Error.Common_PropertyMustBeNonNegative(nameof(configuration.EncryptionAlgorithmKeySize));
        }

        _logger.UsingManagedSymmetricAlgorithm(configuration.EncryptionAlgorithmType.FullName!);

        if (configuration.EncryptionAlgorithmType == typeof(Aes))
        {
            return Aes.Create;
        }
        else
        {
            return AlgorithmActivator.CreateFactory<SymmetricAlgorithm>(configuration.EncryptionAlgorithmType);
        }
    }

    /// <summary>
    /// Contains helper methods for generating cryptographic algorithm factories.
    /// </summary>
    private static class AlgorithmActivator
    {
        /// <summary>
        /// Creates a factory that wraps a call to <see cref="Activator.CreateInstance{T}"/>.
        /// </summary>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe to use because implementation is either a KeyedHashAlgorithm or SymmetricAlgorithm type.")]
        public static Func<T> CreateFactory<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type implementation) where T : class
        {
            return ((IActivator<T>)Activator.CreateInstance(typeof(AlgorithmActivatorCore<>).MakeGenericType(implementation))!).Creator;
        }

        private interface IActivator<out T>
        {
            Func<T> Creator { get; }
        }

        private sealed class AlgorithmActivatorCore<T> : IActivator<T> where T : new()
        {
            public Func<T> Creator { get; } = Activator.CreateInstance<T>;
        }
    }
}
