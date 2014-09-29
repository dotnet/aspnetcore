// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNet.Security.DataProtection.XmlEncryption;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    /// <summary>
    /// Represents a type that contains configuration information about an IAuthenticatedEncryptor
    /// instance, including how to serialize it to XML.
    /// </summary>
    public interface IAuthenticatedEncryptorConfiguration
    {
        /// <summary>
        /// Creates a new IAuthenticatedEncryptor instance based on the current configuration.
        /// </summary>
        /// <returns>An IAuthenticatedEncryptor instance.</returns>
        IAuthenticatedEncryptor CreateEncryptorInstance();

        /// <summary>
        /// Exports the current configuration to XML, optionally encrypting secret key material.
        /// </summary>
        /// <param name="xmlEncryptor">The XML encryptor used to encrypt secret material.</param>
        /// <returns>An XElement representing the current configuration object.</returns>
        XElement ToXml(IXmlEncryptor xmlEncryptor);
    }
}
