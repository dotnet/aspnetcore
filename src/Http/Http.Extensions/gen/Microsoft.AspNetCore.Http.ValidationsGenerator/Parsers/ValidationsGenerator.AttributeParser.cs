// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal static bool ShouldTransformSymbolWithAttribute(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax;
    }

    internal ImmutableArray<ValidatableType> TransformValidatableTypeWithAttribute(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var validatableTypes = new HashSet<ValidatableType>(ValidatableTypeComparer.Instance);
        List<ITypeSymbol> visitedTypes = [];
        var requiredSymbols = ExtractRequiredSymbols(context.SemanticModel.Compilation, cancellationToken);
        if (TryExtractValidatableType((ITypeSymbol)context.TargetSymbol, requiredSymbols, ref validatableTypes, ref visitedTypes))
        {
            return [..validatableTypes];
        }
        return [];
    }
}
