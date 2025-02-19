// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Xml.Linq;
using Microsoft.AspNetCore.OpenApi.SourceGenerators.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

public sealed partial class XmlCommentGenerator
{
    internal static List<(string, string)> ParseXmlFile(AdditionalText additionalText, CancellationToken cancellationToken)
    {
        var text = additionalText.GetText(cancellationToken);
        if (text is null)
        {
            return [];
        }
        XElement xml;
        try
        {
            xml = XElement.Parse(text.ToString());
        }
        catch
        {
            return [];
        }
        var members = xml.Descendants("member");
        var comments = new List<(string, string)>();
        foreach (var member in members)
        {
            var name = member.Attribute(DocumentationCommentXmlNames.NameAttributeName)?.Value;
            if (name is not null)
            {
                comments.Add((name, member.ToString()));
            }
        }
        return comments;
    }

    internal static List<(string, string)> ParseCompilation(Compilation compilation, CancellationToken cancellationToken)
    {
        var visitor = new AssemblyTypeSymbolsVisitor(compilation.Assembly, cancellationToken);
        visitor.VisitAssembly();
        var types = visitor.GetPublicTypes();
        var comments = new List<(string, string)>();
        foreach (var type in types)
        {
            if (DocumentationCommentId.CreateDeclarationId(type) is string name &&
                type.GetDocumentationCommentXml(CultureInfo.InvariantCulture, expandIncludes: true, cancellationToken: cancellationToken) is string xml)
            {
                comments.Add((name, xml));
            }
        }
        var properties = visitor.GetPublicProperties();
        foreach (var property in properties)
        {
            if (DocumentationCommentId.CreateDeclarationId(property) is string name &&
                property.GetDocumentationCommentXml(CultureInfo.InvariantCulture, expandIncludes: true, cancellationToken: cancellationToken) is string xml)
            {
                comments.Add((name, xml));
            }
        }
        var methods = visitor.GetPublicMethods();
        foreach (var method in methods)
        {
            // If the method is a constructor for a record, skip it because we will have already processed the type.
            if (method.MethodKind == MethodKind.Constructor)
            {
                continue;
            }
            if (DocumentationCommentId.CreateDeclarationId(method) is string name &&
                method.GetDocumentationCommentXml(CultureInfo.InvariantCulture, expandIncludes: true, cancellationToken: cancellationToken) is string xml)
            {
                comments.Add((name, xml));
            }
        }
        return comments;
    }

    internal static IEnumerable<(MemberKey, XmlComment?)> ParseComments(
        (List<(string, string)> RawComments, Compilation Compilation) input,
        CancellationToken cancellationToken)
    {
        var compilation = input.Compilation;
        var comments = new List<(MemberKey, XmlComment?)>();
        foreach (var (name, value) in input.RawComments)
        {
            if (DocumentationCommentId.GetFirstSymbolForDeclarationId(name, compilation) is ISymbol symbol)
            {
                var parsedComment = XmlComment.Parse(symbol, compilation, value, cancellationToken);
                if (parsedComment is not null)
                {
                    var memberKey = symbol switch
                    {
                        IMethodSymbol methodSymbol => MemberKey.FromMethodSymbol(methodSymbol, input.Compilation),
                        IPropertySymbol propertySymbol => MemberKey.FromPropertySymbol(propertySymbol),
                        INamedTypeSymbol typeSymbol => MemberKey.FromTypeSymbol(typeSymbol),
                        _ => null
                    };
                    if (memberKey is not null)
                    {
                        comments.Add((memberKey, parsedComment));
                    }
                }
            }
        }
        return comments;
    }

    internal static bool FilterInvocations(SyntaxNode node, CancellationToken _)
        => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddOpenApi" } };

    internal static AddOpenApiInvocation GetAddOpenApiOverloadVariant(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var invocationExpression = (InvocationExpressionSyntax)context.Node;

        // Soft check to validate that the method is from the OpenApiServiceCollectionExtensions class
        // in the Microsoft.AspNetCore.OpenApi assembly.
        var symbol = context.SemanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol;
        if (symbol is not IMethodSymbol methodSymbol
            || methodSymbol.ContainingType.Name != "OpenApiServiceCollectionExtensions"
            || methodSymbol.ContainingAssembly.Name != "Microsoft.AspNetCore.OpenApi")
        {
            return new(AddOpenApiOverloadVariant.Unknown, invocationExpression, null);
        }

        var interceptableLocation = context.SemanticModel.GetInterceptableLocation(invocationExpression, cancellationToken);
        var argumentsCount = invocationExpression.ArgumentList.Arguments.Count;
        if (argumentsCount == 0)
        {
            return new(AddOpenApiOverloadVariant.AddOpenApi, invocationExpression, interceptableLocation);
        }
        else if (argumentsCount == 2)
        {
            return new(AddOpenApiOverloadVariant.AddOpenApiDocumentNameConfigureOptions, invocationExpression, interceptableLocation);
        }
        else
        {
            // We need to disambiguate between the two overloads that take a string and a delegate
            // AddOpenApi("v1") vs. AddOpenApi(options => { }). The implementation here is pretty naive and
            // won't handle cases where the document name is provided by a variable or a method call.
            var argument = invocationExpression.ArgumentList.Arguments[0];
            if (argument.Expression is LiteralExpressionSyntax)
            {
                return new(AddOpenApiOverloadVariant.AddOpenApiDocumentName, invocationExpression, interceptableLocation);
            }
            else if (argument.Expression is LambdaExpressionSyntax)
            {
                return new(AddOpenApiOverloadVariant.AddOpenApiConfigureOptions, invocationExpression, interceptableLocation);
            }
            else
            {
                return new(AddOpenApiOverloadVariant.Unknown, invocationExpression, null);
            }
        }
    }
}
