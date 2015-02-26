// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Represents a type that can deserialize an XML-serialized IAuthenticatedEncryptorConfiguration.
    /// </summary>
    public interface IAuthenticatedEncryptorConfigurationXmlReader
    {
        /// <summary>
        /// Deserializes an XML-serialized IAuthenticatedEncryptorConfiguration.
        /// </summary>
        /// <param name="element">The XML element to deserialize.</param>
        /// <returns>The deserialized IAuthenticatedEncryptorConfiguration.</returns>
        IAuthenticatedEncryptorConfiguration FromXml(XElement element);
    }
}
