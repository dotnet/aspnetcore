// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Xml;

/// <remarks>
/// Source code in this class is derived from Roslyn's XML documentation comment processing
/// to support the full resolution of <inheritdoc /> tags.
/// For the original code, see https://github.com/dotnet/roslyn/blob/ef1d7fe925c94e96a93e4c9af50983e0f675a9fd/src/Workspaces/Core/Portable/Shared/Extensions/ISymbolExtensions.cs.
/// </remarks>
internal sealed partial class XmlComment
{
    private static string? GetDocumentationComment(ISymbol symbol, string xmlText, HashSet<ISymbol>? visitedSymbols, Compilation compilation, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(xmlText))
            {
                if (IsEligibleForAutomaticInheritdoc(symbol))
                {
                    xmlText = $@"<doc><inheritdoc/></doc>";
                }
                else
                {
                    return string.Empty;
                }
            }

            try
            {
                var element = XElement.Parse(xmlText, LoadOptions.PreserveWhitespace);
                element.ReplaceNodes(RewriteMany(symbol, visitedSymbols, compilation, [.. element.Nodes()], cancellationToken));
                xmlText = element.ToString(SaveOptions.DisableFormatting);
            }
            catch (Exception)
            {
                return null;
            }
            return xmlText;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static bool IsEligibleForAutomaticInheritdoc(ISymbol symbol)
    {
        // Only the following symbols are eligible to inherit documentation without an <inheritdoc/> element:
        //
        // * Members that override an inherited member
        // * Members that implement an interface member
        if (symbol.IsOverride)
        {
            return true;
        }

        if (symbol.ContainingType is null)
        {
            // Observed with certain implicit operators, such as operator==(void*, void*).
            return false;
        }

        switch (symbol.Kind)
        {
            case SymbolKind.Method:
            case SymbolKind.Property:
            case SymbolKind.Event:
                if (symbol.ExplicitOrImplicitInterfaceImplementations().Any())
                {
                    return true;
                }

                break;

            default:
                break;
        }

        return false;
    }

    private static XNode[] RewriteMany(ISymbol symbol, HashSet<ISymbol>? visitedSymbols, Compilation compilation, XNode[] nodes, CancellationToken cancellationToken)
    {
        var result = new List<XNode>();
        foreach (var child in nodes)
        {
            result.AddRange(RewriteInheritdocElements(symbol, visitedSymbols, compilation, child, cancellationToken));
        }

        return [.. result];
    }

    private static XNode[] RewriteInheritdocElements(ISymbol symbol, HashSet<ISymbol>? visitedSymbols, Compilation compilation, XNode node, CancellationToken cancellationToken)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            var element = (XElement)node;
            if (ElementNameIs(element, DocumentationCommentXmlNames.InheritdocElementName))
            {
                var rewritten = RewriteInheritdocElement(symbol, visitedSymbols, compilation, element, cancellationToken);
                if (rewritten is not null)
                {
                    return rewritten;
                }
            }
        }

        if (node is not XContainer container)
        {
            return [Copy(node, copyAttributeAnnotations: false)];
        }

        var oldNodes = container.Nodes();

        // Do this after grabbing the nodes, so we don't see copies of them.
        container = Copy(container, copyAttributeAnnotations: false);

        // WARN: don't use node after this point - use container since it's already been copied.

        if (oldNodes != null)
        {
            var rewritten = RewriteMany(symbol, visitedSymbols, compilation, [.. oldNodes], cancellationToken);
            container.ReplaceNodes(rewritten);
        }

        return [container];
    }

    private static XNode[]? RewriteInheritdocElement(ISymbol memberSymbol, HashSet<ISymbol>? visitedSymbols, Compilation compilation, XElement element, CancellationToken cancellationToken)
    {
        var crefAttribute = element.Attribute(XName.Get(DocumentationCommentXmlNames.CrefAttributeName));
        var pathAttribute = element.Attribute(XName.Get(DocumentationCommentXmlNames.PathAttributeName));

        var candidate = GetCandidateSymbol(memberSymbol);
        var hasCandidateCref = candidate is object;

        var hasCrefAttribute = crefAttribute is not null;
        var hasPathAttribute = pathAttribute is not null;
        if (!hasCrefAttribute && !hasCandidateCref)
        {
            // No cref available
            return null;
        }

        ISymbol? symbol;
        if (crefAttribute is null)
        {
            if (candidate is null)
            {
                return null;
            }
            symbol = candidate;
        }
        else
        {
            var crefValue = crefAttribute.Value;
            symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefValue, compilation);
            if (symbol is null)
            {
                return null;
            }
        }

        visitedSymbols ??= [];
        if (!visitedSymbols.Add(symbol!))
        {
            // Prevent recursion
            return null;
        }

        try
        {
            var xmlDocumentation = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
            if (xmlDocumentation is null)
            {
                return [];
            }
            var inheritedDocumentation = GetDocumentationComment(symbol, xmlDocumentation, visitedSymbols, compilation, cancellationToken);
            if (inheritedDocumentation == string.Empty)
            {
                return [];
            }

            var document = XDocument.Parse(inheritedDocumentation, LoadOptions.PreserveWhitespace);
            string xpathValue;
            if (string.IsNullOrEmpty(pathAttribute?.Value))
            {
                xpathValue = BuildXPathForElement(element.Parent!);
            }
            else
            {
                xpathValue = pathAttribute!.Value;
                if (xpathValue.StartsWith("/", StringComparison.InvariantCulture))
                {
                    // Account for the root <doc> or <member> element
                    xpathValue = "/*" + xpathValue;
                }
            }

            // Consider the following code, we want Test<int>.Clone to say "Clones a Test<int>" instead of "Clones a int", thus
            // we rewrite `typeparamref`s as cref pointing to the correct type:
            /*
                public class Test<T> : ICloneable<Test<T>>
                {
                    /// <inheritdoc/>
                    public Test<T> Clone() => new();
                }

                /// <summary>A type that has clonable instances.</summary>
                /// <typeparam name="T">The type of instances that can be cloned.</typeparam>
                public interface ICloneable<T>
                {
                    /// <summary>Clones a <typeparamref name="T"/>.</summary>
                    public T Clone();
                }
            */
            // Note: there is no way to cref an instantiated generic type. See https://github.com/dotnet/csharplang/issues/401
            var typeParameterRefs = document.Descendants(DocumentationCommentXmlNames.TypeParameterReferenceElementName).ToImmutableArray();
            foreach (var typeParameterRef in typeParameterRefs)
            {
                if (typeParameterRef.Attribute(DocumentationCommentXmlNames.NameAttributeName) is XAttribute typeParamName)
                {
                    var targetTypeParameter = symbol.GetAllTypeParameters().FirstOrDefault(p => p.Name == typeParamName.Value);
                    if (targetTypeParameter is not null
                        && symbol.OriginalDefinition.GetAllTypeParameters().IndexOf(targetTypeParameter) is int index
                        && index >= 0)
                    {
                        var typeArgs = symbol.GetAllTypeArguments();
                        if (index < typeArgs.Length)
                        {
                            var docId = typeArgs[index].GetDocumentationCommentId();
                            if (docId != null && !docId.StartsWith("!", StringComparison.OrdinalIgnoreCase))
                            {
                                var replacement = new XElement(DocumentationCommentXmlNames.SeeElementName);
                                replacement.SetAttributeValue(DocumentationCommentXmlNames.CrefAttributeName, docId);
                                typeParameterRef.ReplaceWith(replacement);
                            }
                        }
                    }
                }
            }

            var loadedElements = TrySelectNodes(document, xpathValue);
            return loadedElements ?? [];
        }
        catch (XmlException)
        {
            return [];
        }
        finally
        {
            visitedSymbols.Remove(symbol);
        }

        // Local functions
        static ISymbol? GetCandidateSymbol(ISymbol memberSymbol)
        {
            if (memberSymbol.ExplicitInterfaceImplementations().Any())
            {
                return memberSymbol.ExplicitInterfaceImplementations().First();
            }
            else if (memberSymbol.IsOverride)
            {
                return memberSymbol.GetOverriddenMember();
            }

            if (memberSymbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
                {
                    var baseType = memberSymbol.ContainingType.BaseType;
#nullable disable // Can 'baseType' be null here? https://github.com/dotnet/roslyn/issues/39166
                    return baseType.Constructors.Where(c => IsSameSignature(methodSymbol, c)).FirstOrDefault();
#nullable enable
                }
                else
                {
                    // check for implicit interface
                    return methodSymbol.ExplicitOrImplicitInterfaceImplementations().FirstOrDefault();
                }
            }
            else if (memberSymbol is INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.TypeKind == TypeKind.Class)
                {
                    // Classes use the base type as the default inheritance candidate. A different target (e.g. an
                    // interface) can be provided via the 'path' attribute.
                    return typeSymbol.BaseType;
                }
                else if (typeSymbol.TypeKind == TypeKind.Interface)
                {
                    return typeSymbol.Interfaces.FirstOrDefault();
                }
                else
                {
                    // This includes structs, enums, and delegates as mentioned in the inheritdoc spec
                    return null;
                }
            }

            return memberSymbol.ExplicitOrImplicitInterfaceImplementations().FirstOrDefault();
        }

        static bool IsSameSignature(IMethodSymbol left, IMethodSymbol right)
        {
            if (left.Parameters.Length != right.Parameters.Length)
            {
                return false;
            }

            if (left.IsStatic != right.IsStatic)
            {
                return false;
            }

            if (!SymbolEqualityComparer.Default.Equals(left.ReturnType, right.ReturnType))
            {
                return false;
            }

            for (var i = 0; i < left.Parameters.Length; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(left.Parameters[i].Type, right.Parameters[i].Type))
                {
                    return false;
                }
            }

            return true;
        }

        static string BuildXPathForElement(XElement element)
        {
            if (ElementNameIs(element, "member") || ElementNameIs(element, "doc"))
            {
                // Avoid string concatenation allocations for inheritdoc as a top-level element
                return "/*/node()[not(self::overloads)]";
            }

            var path = "/node()[not(self::overloads)]";
            for (var current = element; current != null; current = current.Parent)
            {
                var currentName = current.Name.ToString();
                if (ElementNameIs(current, "member") || ElementNameIs(current, "doc"))
                {
                    // Allow <member> and <doc> to be used interchangeably
                    currentName = "*";
                }

                path = "/" + currentName + path;
            }

            return path;
        }
    }

    private static bool ElementNameIs(XElement element, string name)
        => string.IsNullOrEmpty(element.Name.NamespaceName) && DocumentationCommentXmlNames.ElementEquals(element.Name.LocalName, name);

    private static TNode Copy<TNode>(TNode node, bool copyAttributeAnnotations)
        where TNode : XNode
    {
        XNode copy;

        // Documents can't be added to containers, so our usual copy trick won't work.
        if (node.NodeType == XmlNodeType.Document)
        {
            copy = new XDocument((XDocument)(object)node);
        }
        else
        {
            XContainer temp = new XElement("temp");
            temp.Add(node);
            copy = temp.LastNode!;
            temp.RemoveNodes();
        }

        AnalyzerDebug.Assert(copy != node);
        AnalyzerDebug.Assert(copy.Parent == null); // Otherwise, when we give it one, it will be copied.

        // Copy annotations, the above doesn't preserve them.
        // We need to preserve Location annotations as well as line position annotations.
        CopyAnnotations(node, copy);

        // We also need to preserve line position annotations for all attributes
        // since we report errors with attribute locations.
        if (copyAttributeAnnotations && node.NodeType == XmlNodeType.Element)
        {
            var sourceElement = (XElement)(object)node;
            var targetElement = (XElement)copy;

            var sourceAttributes = sourceElement.Attributes().GetEnumerator();
            var targetAttributes = targetElement.Attributes().GetEnumerator();
            while (sourceAttributes.MoveNext() && targetAttributes.MoveNext())
            {
                AnalyzerDebug.Assert(sourceAttributes.Current.Name == targetAttributes.Current.Name);
                CopyAnnotations(sourceAttributes.Current, targetAttributes.Current);
            }
        }

        return (TNode)copy;
    }

    private static void CopyAnnotations(XObject source, XObject target)
    {
        foreach (var annotation in source.Annotations<object>())
        {
            target.AddAnnotation(annotation);
        }
    }

    private static XNode[]? TrySelectNodes(XNode node, string xpath)
    {
        try
        {
            var xpathResult = (IEnumerable)System.Xml.XPath.Extensions.XPathEvaluate(node, xpath);

            // Throws InvalidOperationException if the result of the XPath is an XDocument:
            return xpathResult?.Cast<XNode>().ToArray();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (XPathException)
        {
            return null;
        }
    }
}
