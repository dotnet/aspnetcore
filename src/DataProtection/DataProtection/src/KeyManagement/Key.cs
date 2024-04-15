// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// The basic implementation of <see cref="IKey"/>.
/// </summary>
internal sealed class Key : IKey
{
    private readonly Lazy<IAuthenticatedEncryptorDescriptor> _lazyDescriptor;
    private readonly IEnumerable<IAuthenticatedEncryptorFactory> _encryptorFactories;

    private IAuthenticatedEncryptor? _encryptor;

    /// <summary>
    /// The basic implementation of <see cref="IKey"/>, where the <see cref="IAuthenticatedEncryptorDescriptor"/>
    /// has already been created.
    /// </summary>
    public Key(
        Guid keyId,
        DateTimeOffset creationDate,
        DateTimeOffset activationDate,
        DateTimeOffset expirationDate,
        IAuthenticatedEncryptorDescriptor descriptor,
        IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories)
        : this(keyId,
              creationDate,
              activationDate,
              expirationDate,
              new Lazy<IAuthenticatedEncryptorDescriptor>(() => descriptor),
              encryptorFactories)
    {
    }

    /// <summary>
    /// The basic implementation of <see cref="IKey"/>, where the incoming XML element
    /// hasn't yet been fully processed.
    /// </summary>
    public Key(
        Guid keyId,
        DateTimeOffset creationDate,
        DateTimeOffset activationDate,
        DateTimeOffset expirationDate,
        IInternalXmlKeyManager keyManager,
        XElement keyElement,
        IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories)
        : this(keyId,
              creationDate,
              activationDate,
              expirationDate,
              new Lazy<IAuthenticatedEncryptorDescriptor>(GetLazyDescriptorDelegate(keyManager, keyElement)),
              encryptorFactories)
    {
    }

    private Key(
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

    internal Key Clone()
    {
        return new Key(
            keyId: KeyId,
            creationDate: CreationDate,
            activationDate: ActivationDate,
            expirationDate: ExpirationDate,
            lazyDescriptor: _lazyDescriptor,
            encryptorFactories: _encryptorFactories)
        {
            IsRevoked = IsRevoked,
        };
    }

    private static Func<IAuthenticatedEncryptorDescriptor> GetLazyDescriptorDelegate(IInternalXmlKeyManager keyManager, XElement keyElement)
    {
        // The <key> element will be held around in memory for a potentially lengthy period
        // of time. Since it might contain sensitive information, we should protect it.
        var encryptedKeyElement = keyElement.ToSecret();

        try
        {
            return GetLazyDescriptorDelegate;
        }
        finally
        {
            // It's important that the lambda above doesn't capture 'descriptorElement'. Clearing the reference here
            // helps us detect if we've done this by causing a null ref at runtime.
            keyElement = null!;
        }

        IAuthenticatedEncryptorDescriptor GetLazyDescriptorDelegate()
        {
            return keyManager.DeserializeDescriptorFromKeyElement(encryptedKeyElement.ToXElement());
        }
    }
}
