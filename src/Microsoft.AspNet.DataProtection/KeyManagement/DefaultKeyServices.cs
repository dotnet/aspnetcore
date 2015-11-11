// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.DataProtection.KeyManagement.Internal;
using Microsoft.AspNet.DataProtection.Repositories;
using Microsoft.AspNet.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    internal sealed class DefaultKeyServices : IDefaultKeyServices
    {
        private readonly Lazy<object> _keyEncryptorLazy;
        private readonly Lazy<object> _keyRepositoryLazy;

        public DefaultKeyServices(IServiceProvider services, ServiceDescriptor keyEncryptorDescriptor, ServiceDescriptor keyRepositoryDescriptor)
        {
            if (keyEncryptorDescriptor != null)
            {
                // optional
                CryptoUtil.Assert(keyEncryptorDescriptor.ServiceType == typeof(IXmlEncryptor), "Bad service type.");
                _keyEncryptorLazy = GetLazyForService(services, keyEncryptorDescriptor);
            }

            CryptoUtil.Assert(keyRepositoryDescriptor.ServiceType == typeof(IXmlRepository), "Bad service type.");
            _keyRepositoryLazy = GetLazyForService(services, keyRepositoryDescriptor);
        }

        /// <summary>
        /// Gets the default <see cref="IXmlEncryptor"/> service (could return null).
        /// </summary>
        /// <returns></returns>
        public IXmlEncryptor GetKeyEncryptor()
        {
            return (IXmlEncryptor)_keyEncryptorLazy?.Value;
        }

        /// <summary>
        /// Gets the default <see cref="IXmlRepository"/> service (must not be null).
        /// </summary>
        /// <returns></returns>
        public IXmlRepository GetKeyRepository()
        {
            return (IXmlRepository)_keyRepositoryLazy.Value ?? CryptoUtil.Fail<IXmlRepository>("GetKeyRepository returned null.");
        }

        private static Lazy<object> GetLazyForService(IServiceProvider services, ServiceDescriptor descriptor)
        {
            CryptoUtil.Assert(descriptor != null && descriptor.Lifetime == ServiceLifetime.Singleton, "Descriptor must represent singleton.");
            CryptoUtil.Assert(descriptor.ImplementationFactory != null, "Descriptor must have an implementation factory.");

            // pull the factory out so we don't close over the whole descriptor instance
            Func<IServiceProvider, object> wrapped = descriptor.ImplementationFactory;
            return new Lazy<object>(() => wrapped(services));
        }
    }
}
