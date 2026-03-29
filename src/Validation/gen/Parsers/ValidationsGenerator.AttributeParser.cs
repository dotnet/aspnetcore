// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.Validation;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal static bool ShouldTransformSymbolWithAttribute(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax or RecordDeclarationSyntax;
    }

    internal TypeSymbolExtraction ExtractValidatableTypeWithAttributeSymbol(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.SemanticModel.Compilation);
        return new((ITypeSymbol)context.TargetSymbol, wellKnownTypes);
    }

    internal ImmutableArray<ValidatableType> RetriveValidatableTypes((TypeSymbolExtraction extraction, GeneratorConfiguration configuration) input, CancellationToken cancellationToken)
    {
        var validatableTypes = new HashSet<ValidatableType>(ValidatableTypeComparer.Instance);
        List<ITypeSymbol> visitedTypes = [];
        if (TryExtractValidatableType(input.extraction.symbol, input.extraction.wellKnownTypes, ref validatableTypes, ref visitedTypes, input.configuration))
        {
            return [.. validatableTypes];
        }
        return [];
    }
}
