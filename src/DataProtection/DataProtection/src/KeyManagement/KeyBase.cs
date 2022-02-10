// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// The basic implementation of <see cref="IKey"/>.
/// </summary>
internal abstract class KeyBase : IKey
{
    private readonly Lazy<IAuthenticatedEncryptorDescriptor> _lazyDescriptor;
    private readonly IEnumerable<IAuthenticatedEncryptorFactory> _encryptorFactories;

    private IAuthenticatedEncryptor? _encryptor;

    public KeyBase(
        Guid keyId,
        DateTimeOffset creationDate,
        DateTimeOffset activationDate,
        DateTimeOffset expirationDate,
        Lazy<IAuthenticatedEncryptorDescriptor> lazyDescriptor,
        IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories)
    {
        KeyId = keyId;
        CreationDate = creationDate;
        ActivationDate = activationDate;
        ExpirationDate = expirationDate;
        _lazyDescriptor = lazyDescriptor;
        _encryptorFactories = encryptorFactories;
    }

    public DateTimeOffset ActivationDate { get; }

    public DateTimeOffset CreationDate { get; }

    public DateTimeOffset ExpirationDate { get; }

    public bool IsRevoked { get; private set; }

    public Guid KeyId { get; }

    public IAuthenticatedEncryptorDescriptor Descriptor
    {
        get
        {
            return _lazyDescriptor.Value;
        }
    }

    public IAuthenticatedEncryptor? CreateEncryptor()
    {
        if (_encryptor == null)
        {
            foreach (var factory in _encryptorFactories)
            {
                var encryptor = factory.CreateEncryptorInstance(this);
                if (encryptor != null)
                {
                    _encryptor = encryptor;
                    break;
                }
            }
        }

        return _encryptor;
    }

    internal void SetRevoked()
    {
        IsRevoked = true;
    }
}
