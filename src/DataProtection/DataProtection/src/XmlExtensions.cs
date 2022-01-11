// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Contains helpers to work with XElement objects.
/// </summary>
internal static class XmlExtensions
{
    /// <summary>
    /// Returns a new XElement which is a carbon copy of the provided element,
    /// but with no child nodes. Useful for writing exception messages without
    /// inadvertently disclosing secret key material. It is assumed that the
    /// element name itself and its attribute values are not secret.
    /// </summary>
    public static XElement WithoutChildNodes(this XElement element)
    {
        var newElement = new XElement(element.Name);
        foreach (var attr in element.Attributes())
        {
            newElement.SetAttributeValue(attr.Name, attr.Value);
        }
        return newElement;
    }
}
