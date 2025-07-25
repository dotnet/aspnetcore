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

    internal ImmutableArray<ValidatableType> TransformValidatableTypeWithAttribute(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var validatableTypes = new HashSet<ValidatableType>(ValidatableTypeComparer.Instance);
        List<ITypeSymbol> visitedTypes = [];
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.SemanticModel.Compilation);
        if (TryExtractValidatableType((ITypeSymbol)context.TargetSymbol, wellKnownTypes, ref validatableTypes, ref visitedTypes))
        {
            return [..validatableTypes];
        }
        return [];
    }
}
