// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Xunit assertions related to Xml content.
/// </summary>
public static class XmlAssert
{
    /// <summary>
    /// Compares two xml strings ignoring an element's attribute order.
    /// </summary>
    /// <param name="expectedXml">Expected xml string.</param>
    /// <param name="actualXml">Actual xml string.</param>
    public static void Equal(string expectedXml, string actualXml)
    {
        var sortedExpectedXDocument = SortAttributes(XDocument.Parse(expectedXml));
        var sortedActualXDocument = SortAttributes(XDocument.Parse(actualXml));

        // Since XNode's DeepEquals does not check for presence of xml declaration,
        // check it explicitly
        bool areEqual = EqualDeclarations(sortedExpectedXDocument.Declaration, sortedActualXDocument.Declaration);

        areEqual = areEqual && XNode.DeepEquals(sortedExpectedXDocument, sortedActualXDocument);

        if (!areEqual)
        {
            throw EqualException.ForMismatchedValues(
                sortedExpectedXDocument.ToString(SaveOptions.DisableFormatting),
                sortedActualXDocument.ToString(SaveOptions.DisableFormatting));
        }
    }

    private static bool EqualDeclarations(XDeclaration expected, XDeclaration actual)
    {
        if (expected == null && actual == null)
        {
            return true;
        }

        if (expected == null || actual == null)
        {
            return false;
        }

        // Note that this ignores 'Standalone' property comparison.
        return string.Equals(expected.Version, actual.Version, StringComparison.OrdinalIgnoreCase)
            && string.Equals(expected.Encoding, actual.Encoding, StringComparison.OrdinalIgnoreCase);
    }

    private static XDocument SortAttributes(XDocument document)
    {
        return new XDocument(
            document.Declaration,
            SortAttributes(document.Root));
    }

    private static XNode SortAttributes(XNode node)
    {
        XElement element = node as XElement;
        if (element == null)
        {
            return node;
        }

        return new XElement(
            element.Name,
            element.Attributes().OrderBy(a => a.Name.ToString()),
            element.Nodes().Select(child => SortAttributes(child)));
    }
}
