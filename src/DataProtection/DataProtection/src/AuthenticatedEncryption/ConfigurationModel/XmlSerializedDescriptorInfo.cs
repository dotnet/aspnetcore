// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

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
        ArgumentNullThrowHelper.ThrowIfNull(serializedDescriptorElement);
        ArgumentNullThrowHelper.ThrowIfNull(deserializerType);

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
