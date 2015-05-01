// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A self-contained descriptor that wraps all information (including secret key
    /// material) necessary to create an instance of an <see cref="IAuthenticatedEncryptor"/>.
    /// </summary>
    public interface IAuthenticatedEncryptorDescriptor
    {
        /// <summary>
        /// Creates an <see cref="IAuthenticatedEncryptor"/> instance based on the current descriptor.
        /// </summary>
        /// <returns>An <see cref="IAuthenticatedEncryptor"/> instance.</returns>
        /// <remarks>
        /// For a given descriptor, any two instances returned by this method should
        /// be considered equivalent, e.g., the payload returned by one's <see cref="IAuthenticatedEncryptor.Encrypt(ArraySegment{byte}, ArraySegment{byte})"/>
        /// method should be consumable by the other's <see cref="IAuthenticatedEncryptor.Decrypt(ArraySegment{byte}, ArraySegment{byte})"/> method.
        /// </remarks>
        IAuthenticatedEncryptor CreateEncryptorInstance();

        /// <summary>
        /// Exports the current descriptor to XML.
        /// </summary>
        /// <returns>
        /// An <see cref="XmlSerializedDescriptorInfo"/> wrapping the <see cref="XElement"/> which represents the serialized
        /// current descriptor object. The deserializer type must be assignable to <see cref="IAuthenticatedEncryptorDescriptorDeserializer"/>.
        /// </returns>
        /// <remarks>
        /// If an element contains sensitive information (such as key material), the
        /// element should be marked via the <see cref="XmlExtensions.MarkAsRequiresEncryption(XElement)" />
        /// extension method, and the caller should encrypt the element before persisting
        /// the XML to storage.
        /// </remarks>
        XmlSerializedDescriptorInfo ExportToXml();
    }
}
