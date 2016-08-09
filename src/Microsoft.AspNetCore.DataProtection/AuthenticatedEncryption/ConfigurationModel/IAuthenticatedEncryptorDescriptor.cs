// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A self-contained descriptor that wraps all information (including secret key
    /// material) necessary to create an instance of an <see cref="IAuthenticatedEncryptor"/>.
    /// </summary>
    public interface IAuthenticatedEncryptorDescriptor
    {
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
