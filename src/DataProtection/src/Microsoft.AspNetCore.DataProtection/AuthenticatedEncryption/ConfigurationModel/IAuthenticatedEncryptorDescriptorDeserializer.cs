// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// The basic interface for deserializing an XML element into an <see cref="IAuthenticatedEncryptorDescriptor"/>.
    /// </summary>
    public interface IAuthenticatedEncryptorDescriptorDeserializer
    {
        /// <summary>
        /// Deserializes the specified XML element.
        /// </summary>
        /// <param name="element">The element to deserialize.</param>
        /// <returns>The <see cref="IAuthenticatedEncryptorDescriptor"/> represented by <paramref name="element"/>.</returns>
        IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element);
    }
}
