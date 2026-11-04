// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

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
