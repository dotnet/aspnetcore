// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Validation;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ValidatableTypeInGeneratedCodeDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private const string Usage = "Usage";

    internal static readonly DiagnosticDescriptor ValidatableTypeCantBeUsedInGeneratedCode = new(
        "ASP0037",
        "[ValidatableType] cannot be used in generated code",
        "'[ValidatableType]' on type '{0}' has no effect because the type is declared in generated code (for example, in a .razor file). Source generators cannot inspect each other's output. Declare the type in a regular .cs file instead.",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: "https://learn.microsoft.com/aspnet/core/diagnostics/asp0037");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [
            ValidatableTypeCantBeUsedInGeneratedCode,
        ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(context =>
        {
            var validatableTypeAttribute = context.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Validation.ValidatableTypeAttribute");
            if (validatableTypeAttribute is null)
            {
                return;
            }

            context.RegisterOperationAction(context =>
            {
                if (!context.IsGeneratedCode)
                {
                    return;
                }

                var attributeOperation = (IAttributeOperation)context.Operation;
                if (context.ContainingSymbol is INamedTypeSymbol attributedType &&
                    attributeOperation.Operation is IObjectCreationOperation attributeObjectCreationOperation &&
                    validatableTypeAttribute.Equals(attributeObjectCreationOperation.Constructor?.ContainingType, SymbolEqualityComparer.Default))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ValidatableTypeCantBeUsedInGeneratedCode,
                        attributedType.Locations.FirstOrDefault(),
                        attributedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                }
            }, OperationKind.Attribute);
        });
    }
}
