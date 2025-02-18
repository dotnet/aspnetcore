// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Xml;

internal sealed partial class XmlComment
{
    public string? Summary { get; internal set; }
    public string? Description { get; internal set; }
    public string? Value { get; internal set; }
    public string? Remarks { get; internal set; }
    public string? Returns { get; internal set; }
    public bool? Deprecated { get; internal set; }
    public List<string?>? Examples { get; internal set; }
    public List<XmlParameterComment> Parameters { get; internal set; } = [];
    public List<XmlResponseComment> Responses { get; internal set; } = [];

    private XmlComment(Compilation compilation, string xml)
    {
        // Treat <doc> as <member>
        if (xml.StartsWith("<doc>", StringComparison.InvariantCulture) && xml.EndsWith("</doc>", StringComparison.InvariantCulture))
        {
            xml = xml.Substring(5, xml.Length - 11);
            xml = xml.Trim();
        }

        // Transform triple slash comment
        var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        ResolveCrefLink(compilation, doc, $"//{DocumentationCommentXmlNames.SeeAlsoElementName}[@cref]");
        ResolveCrefLink(compilation, doc, $"//{DocumentationCommentXmlNames.SeeElementName}[@cref]");
        // Resolve <list> and <item> tags into bullets
        ResolveListTags(doc);
        // Resolve <code> tags into code blocks
        ResolveCodeTags(doc, DocumentationCommentXmlNames.CodeElementName, "```");
        // Resolve <paramref> and typeparamref tags into parameter names
        ResolveParamRefTags(doc);
        // Resolve <para> tags into underlying content
        ResolveParaTags(doc);
        // Resolve <c> tags into inline code blocks
        ResolveCodeTags(doc, DocumentationCommentXmlNames.CElementName, "`");

        var nav = doc.CreateNavigator();
        Summary = GetSingleNodeValue(nav, "/member/summary");
        Description = GetSingleNodeValue(nav, "/member/description");
        Remarks = GetSingleNodeValue(nav, "/member/remarks");
        Returns = GetSingleNodeValue(nav, "/member/returns");
        Value = GetSingleNodeValue(nav, "/member/value");
        Deprecated = GetSingleNodeValue(nav, "/member/deprecated") == "true";

        Examples = [.. GetMultipleExampleNodes(nav, "/member/example")];
        Parameters = XmlParameterComment.GetXmlParameterListComment(nav, "/member/param");
        Responses = XmlResponseComment.GetXmlResponseCommentList(nav, "/member/response");
    }

    private static void ResolveListTags(XDocument document)
    {
        var listElements = document.Descendants(DocumentationCommentXmlNames.ListElementName).ToArray();
        foreach (var element in listElements)
        {
            if (element is null)
            {
                continue;
            }
            var rawListType = element.Attribute(DocumentationCommentXmlNames.TypeAttributeName)?.Value;
            var listPrefix = rawListType switch
            {
                "table" => "* ",
                "number" => "1. ",
                "bullet" => "* ",
                _ => "* ",
            };
            var items = element.Elements(DocumentationCommentXmlNames.ItemElementName);
            if (items == null)
            {
                continue;
            }
            var bulletPoints = items
                .Select(item => listPrefix + item?.Value?.TrimEachLine() ?? string.Empty)
                .ToList();

            var bulletText = string.Join("\n", bulletPoints);
            element.ReplaceWith(new XText(bulletText));
        }
    }

    private static void ResolveCodeTags(XDocument document, string tagName, string codeBlockDelimiter)
    {
        var codeElements = document.Descendants(tagName).ToArray();
        foreach (var element in codeElements)
        {
            if (element is null)
            {
                continue;
            }
            var codeText = element.Value.TrimEachLine();
            element.ReplaceWith(new XText(codeBlockDelimiter + codeText + codeBlockDelimiter));
        }
    }

    private static void ResolveParamRefTags(XDocument document)
    {
        var paramRefElements = document.Descendants(DocumentationCommentXmlNames.ParameterReferenceElementName).ToArray();
        foreach (var element in paramRefElements)
        {
            if (element is null)
            {
                continue;
            }
            var paramName = element.Attribute(DocumentationCommentXmlNames.NameAttributeName)?.Value;
            if (paramName is null)
            {
                continue;
            }
            element.ReplaceWith(new XText(paramName));
        }

        var typeParamRefElements = document.Descendants(DocumentationCommentXmlNames.TypeParameterReferenceElementName).ToArray();
        foreach (var element in typeParamRefElements)
        {
            if (element is null)
            {
                continue;
            }
            var paramName = element.Attribute(DocumentationCommentXmlNames.NameAttributeName)?.Value;
            if (paramName is null)
            {
                continue;
            }
            element.ReplaceWith(new XText(paramName));
        }
    }

    private static void ResolveParaTags(XDocument document)
    {
        var paraElements = document.Descendants(DocumentationCommentXmlNames.ParaElementName).ToArray();
        foreach (var element in paraElements)
        {
            if (element is null)
            {
                continue;
            }
            var paraText = element.Value.TrimEachLine();
            element.ReplaceWith(new XText(paraText));
        }
    }

    public static XmlComment? Parse(ISymbol symbol, Compilation compilation, string xmlText, CancellationToken cancellationToken)
    {
        // Avoid processing empty or malformed XML comments.
        if (string.IsNullOrEmpty(xmlText) ||
            xmlText.StartsWith("<!-- Badly formed XML comment ignored for member ", StringComparison.Ordinal))
        {
            return null;
        }

        var resolvedComment = GetDocumentationComment(symbol, xmlText, [], compilation, cancellationToken);
        return !string.IsNullOrEmpty(resolvedComment) ? new XmlComment(compilation, resolvedComment!) : null;
    }

    /// <summary>
    /// Resolves the cref links in the XML documentation into type names.
    /// </summary>
    /// <param name="compilation">The compilation to resolve type symbol declarations from.</param>
    /// <param name="node">The target node to process crefs in.</param>
    /// <param name="nodeSelector">The node type to process crefs for, can be `see` or `seealso`.</param>
    private static void ResolveCrefLink(Compilation compilation, XNode node, string nodeSelector)
    {
        if (node == null || string.IsNullOrEmpty(nodeSelector))
        {
            return;
        }

        var nodes = node.XPathSelectElements(nodeSelector + "[@cref]").ToArray();
        foreach (var item in nodes)
        {
            var cref = item.Attribute(DocumentationCommentXmlNames.CrefAttributeName).Value;
            if (string.IsNullOrEmpty(cref))
            {
                continue;
            }

            var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(cref, compilation);
            if (symbol is not null)
            {
                var type = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                item.ReplaceWith(new XText(type));
            }
        }
    }

    private static IEnumerable<string?> GetMultipleExampleNodes(XPathNavigator navigator, string selector)
    {
        var iterator = navigator.Select(selector);
        if (iterator == null)
        {
            yield break;
        }
        foreach (XPathNavigator nav in iterator)
        {
            yield return nav.InnerXml.TrimEachLine();
        }
    }

    private static string? GetSingleNodeValue(XPathNavigator nav, string selector)
    {
        var node = nav.Clone().SelectSingleNode(selector);
        return node?.InnerXml.TrimEachLine();
    }
}
