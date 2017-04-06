// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    /// <summary>
    /// The basic implementation of <see cref="IKey"/>, where the incoming XML element
    /// hasn't yet been fully processed.
    /// </summary>
    internal sealed class DeferredKey : KeyBase
    {
        public DeferredKey(
            Guid keyId,
            DateTimeOffset creationDate,
            DateTimeOffset activationDate,
            DateTimeOffset expirationDate,
            IInternalXmlKeyManager keyManager,
            XElement keyElement,
            IEnumerable<IAuthenticatedEncryptorFactory> encryptorFactories)
            : base(keyId,
                  creationDate,
                  activationDate,
                  expirationDate,
                  new Lazy<IAuthenticatedEncryptorDescriptor>(GetLazyDescriptorDelegate(keyManager, keyElement)),
                  encryptorFactories)
        {
        }

        private static Func<IAuthenticatedEncryptorDescriptor> GetLazyDescriptorDelegate(IInternalXmlKeyManager keyManager, XElement keyElement)
        {
            // The <key> element will be held around in memory for a potentially lengthy period
            // of time. Since it might contain sensitive information, we should protect it.
            var encryptedKeyElement = keyElement.ToSecret();

            try
            {
                return () => keyManager.DeserializeDescriptorFromKeyElement(encryptedKeyElement.ToXElement());
            }
            finally
            {
                // It's important that the lambda above doesn't capture 'descriptorElement'. Clearing the reference here
                // helps us detect if we've done this by causing a null ref at runtime.
                keyElement = null;
            }
        }
    }
}
