// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Wraps an <see cref="XElement"/> that contains the XML-serialized representation of an
    /// <see cref="IAuthenticatedEncryptorDescriptor"/> along with the type that can be used
    /// to deserialize it.
    /// </summary>
    public sealed class XmlSerializedDescriptorInfo
    {
        /// <summary>
        /// Creates an instance of an <see cref="XmlSerializedDescriptorInfo"/>.
        /// </summary>
        /// <param name="serializedDescriptorElement">The XML-serialized form of the <see cref="IAuthenticatedEncryptorDescriptor"/>.</param>
        /// <param name="deserializerType">The class whose <see cref="IAuthenticatedEncryptorDescriptorDeserializer.ImportFromXml(XElement)"/>
        /// method can be used to deserialize <paramref name="serializedDescriptorElement"/>.</param>
        public XmlSerializedDescriptorInfo(XElement serializedDescriptorElement, Type deserializerType)
        {
            if (serializedDescriptorElement == null)
            {
                throw new ArgumentNullException(nameof(serializedDescriptorElement));
            }

            if (deserializerType == null)
            {
                throw new ArgumentNullException(nameof(deserializerType));
            }

            if (!typeof(IAuthenticatedEncryptorDescriptorDeserializer).IsAssignableFrom(deserializerType))
            {
                throw new ArgumentException(
                    Resources.FormatTypeExtensions_BadCast(deserializerType.FullName, typeof(IAuthenticatedEncryptorDescriptorDeserializer).FullName),
                    nameof(deserializerType));
            }

            SerializedDescriptorElement = serializedDescriptorElement;
            DeserializerType = deserializerType;
        }

        /// <summary>
        /// The class whose <see cref="IAuthenticatedEncryptorDescriptorDeserializer.ImportFromXml(XElement)"/>
        /// method can be used to deserialize the value stored in <see cref="SerializedDescriptorElement"/>.
        /// </summary>
        public Type DeserializerType { get; }

        /// <summary>
        /// An XML-serialized representation of an <see cref="IAuthenticatedEncryptorDescriptor"/>.
        /// </summary>
        public XElement SerializedDescriptorElement { get; }
    }
}
