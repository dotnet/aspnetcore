// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

/// <summary>
/// Data protection extensions for <see cref="XElement"/>.
/// </summary>
public static class XmlExtensions
{
    internal static bool IsMarkedAsRequiringEncryption(this XElement element)
    {
        return ((bool?)element.Attribute(XmlConstants.RequiresEncryptionAttributeName)).GetValueOrDefault();
    }

    /// <summary>
    /// Marks the provided <see cref="XElement"/> as requiring encryption before being persisted
    /// to storage. Use when implementing <see cref="IAuthenticatedEncryptorDescriptor.ExportToXml"/>.
    /// </summary>
    public static void MarkAsRequiresEncryption(this XElement element)
    {
        ArgumentNullThrowHelper.ThrowIfNull(element);

        element.SetAttributeValue(XmlConstants.RequiresEncryptionAttributeName, true);
    }
}
