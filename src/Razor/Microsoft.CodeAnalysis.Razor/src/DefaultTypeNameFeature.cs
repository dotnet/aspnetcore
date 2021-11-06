// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Razor;

internal class DefaultTypeNameFeature : TypeNameFeature
{
    public override IReadOnlyList<string> ParseTypeParameters(string typeName)
    {
        if (typeName == null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        var parsed = SyntaxFactory.ParseTypeName(typeName);
        if (parsed is IdentifierNameSyntax identifier)
        {
            return Array.Empty<string>();
        }
        else if (parsed is ArrayTypeSyntax array)
        {
            return new[] { array.ElementType.ToString() };
        }
        else if (parsed is TupleTypeSyntax tuple)
        {
            return tuple.Elements.Select(a => a.ToString()).ToList();
        }
        else
        {
            return parsed.DescendantNodesAndSelf()
                .OfType<TypeArgumentListSyntax>()
                .SelectMany(arg => arg.Arguments)
                .Select(a => a.ToString()).ToList();
        }
    }

    public override TypeNameRewriter CreateGenericTypeRewriter(Dictionary<string, string> bindings)
    {
        if (bindings == null)
        {
            throw new ArgumentNullException(nameof(bindings));
        }

        return new GenericTypeNameRewriter(bindings);
    }

    public override TypeNameRewriter CreateGlobalQualifiedTypeNameRewriter(ICollection<string> ignore)
    {
        if (ignore == null)
        {
            throw new ArgumentNullException(nameof(ignore));
        }

        return new GlobalQualifiedTypeNameRewriter(ignore);
    }

    public override bool IsLambda(string expression)
    {
        var parsed = SyntaxFactory.ParseExpression(expression);
        return parsed is LambdaExpressionSyntax;
    }
}
