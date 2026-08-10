// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Analyzer that detects Virtualize spacer elements that are invalid for their HTML parent.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VirtualizeSpacerElementAnalyzer : DiagnosticAnalyzer
{
    private const string VirtualizeTypeName = "Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize`1";
    private const string RenderTreeBuilderTypeName = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";

    private static readonly ImmutableDictionary<string, ImmutableArray<string>> AllowedSpacerElements =
        new Dictionary<string, ImmutableArray<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["tbody"] = ImmutableArray.Create("tr"),
            ["thead"] = ImmutableArray.Create("tr"),
            ["tfoot"] = ImmutableArray.Create("tr"),
            ["ul"] = ImmutableArray.Create("li"),
            ["ol"] = ImmutableArray.Create("li"),
            ["tr"] = ImmutableArray.Create("td", "th"),
            ["select"] = ImmutableArray.Create("option"),
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var virtualizeType = compilationContext.Compilation.GetTypeByMetadataName(VirtualizeTypeName);
            var renderTreeBuilderType = compilationContext.Compilation.GetTypeByMetadataName(RenderTreeBuilderTypeName);

            if (virtualizeType is null || renderTreeBuilderType is null)
            {
                return;
            }

            compilationContext.RegisterOperationBlockAction(blockContext =>
            {
                var renderTreeStack = new Stack<RenderTreeFrame>();

                foreach (var operationBlock in blockContext.OperationBlocks)
                {
                    foreach (var operation in operationBlock.DescendantsAndSelf())
                    {
                        if (operation is not IInvocationOperation invocation)
                        {
                            continue;
                        }

                        if (!SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, renderTreeBuilderType))
                        {
                            AnalyzeGeneratedHelperInvocation(
                                blockContext,
                                invocation,
                                renderTreeStack,
                                virtualizeType,
                                renderTreeBuilderType);
                            continue;
                        }

                        switch (invocation.TargetMethod.Name)
                        {
                            case "OpenElement":
                                renderTreeStack.Push(new RenderTreeFrame
                                {
                                    ElementName = GetConstantStringArgument(invocation, 1),
                                });
                                break;

                            case "OpenComponent":
                                var isVirtualize = invocation.TargetMethod.IsGenericMethod &&
                                    invocation.TargetMethod.TypeArguments.Length == 1 &&
                                    invocation.TargetMethod.TypeArguments[0] is INamedTypeSymbol componentType &&
                                    SymbolEqualityComparer.Default.Equals(componentType.OriginalDefinition, virtualizeType);

                                renderTreeStack.Push(new RenderTreeFrame
                                {
                                    IsComponent = true,
                                    IsVirtualize = isVirtualize,
                                    ParentElementName = isVirtualize && renderTreeStack.Count > 0 && !renderTreeStack.Peek().IsComponent
                                        ? renderTreeStack.Peek().ElementName
                                        : null,
                                    Location = invocation.Syntax.GetLocation(),
                                });
                                break;

                            case "AddComponentParameter":
                                if (renderTreeStack.Count > 0 &&
                                    renderTreeStack.Peek() is { IsVirtualize: true } virtualizeFrame &&
                                    string.Equals(GetConstantStringArgument(invocation, 1), "SpacerElement", StringComparison.Ordinal))
                                {
                                    virtualizeFrame.HasSpacerElement = true;
                                    virtualizeFrame.SpacerElement = GetConstantStringArgument(invocation, 2);
                                }
                                break;

                            case "CloseComponent":
                                if (renderTreeStack.Count > 0)
                                {
                                    var componentFrame = renderTreeStack.Pop();
                                    ReportInvalidSpacerElement(blockContext, componentFrame);
                                }
                                break;

                            case "CloseElement":
                                if (renderTreeStack.Count > 0)
                                {
                                    renderTreeStack.Pop();
                                }
                                break;
                        }
                    }
                }
            });
        });
    }

    private static void AnalyzeGeneratedHelperInvocation(
        OperationBlockAnalysisContext context,
        IInvocationOperation invocation,
        Stack<RenderTreeFrame> renderTreeStack,
        INamedTypeSymbol virtualizeType,
        INamedTypeSymbol renderTreeBuilderType)
    {
        if (renderTreeStack.Count == 0 ||
            renderTreeStack.Peek().IsComponent ||
            renderTreeStack.Peek().ElementName is not { } parentElementName ||
            !AllowedSpacerElements.ContainsKey(parentElementName) ||
            !TryGetVirtualizeHelperInfo(
                context.Compilation,
                invocation.TargetMethod,
                virtualizeType,
                renderTreeBuilderType,
                context.CancellationToken,
                out var helperInfo))
        {
            return;
        }

        string? spacerElement = null;
        if (helperInfo.HasSpacerElement)
        {
            if (helperInfo.SpacerParameterOrdinal is not { } spacerParameterOrdinal)
            {
                return;
            }

            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter?.Ordinal == spacerParameterOrdinal)
                {
                    var constantValue = UnwrapConversion(argument.Value).ConstantValue;
                    if (!constantValue.HasValue || constantValue.Value is not string value)
                    {
                        return;
                    }

                    spacerElement = value;
                    break;
                }
            }
        }

        ReportInvalidSpacerElement(context, new RenderTreeFrame
        {
            IsVirtualize = true,
            HasSpacerElement = helperInfo.HasSpacerElement,
            ParentElementName = parentElementName,
            SpacerElement = spacerElement,
            Location = invocation.Syntax.GetLocation(),
        });
    }

    private static bool TryGetVirtualizeHelperInfo(
        Compilation compilation,
        IMethodSymbol method,
        INamedTypeSymbol virtualizeType,
        INamedTypeSymbol renderTreeBuilderType,
        CancellationToken cancellationToken,
        out VirtualizeHelperInfo helperInfo)
    {
        helperInfo = default;

        if (!string.Equals(method.ContainingType.Name, "TypeInference", StringComparison.Ordinal) ||
            !method.Name.StartsWith("CreateVirtualize_", StringComparison.Ordinal) ||
            method.DeclaringSyntaxReferences.Length != 1 ||
            method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not MethodDeclarationSyntax methodDeclaration ||
            methodDeclaration.Body is null)
        {
            return false;
        }

        var semanticModel = compilation.GetSemanticModel(methodDeclaration.SyntaxTree);
        if (semanticModel.GetOperation(methodDeclaration.Body, cancellationToken) is not { } methodBody)
        {
            return false;
        }

        var createsVirtualize = false;
        int? spacerParameterOrdinal = null;
        var hasSpacerElement = false;

        foreach (var operation in methodBody.DescendantsAndSelf())
        {
            if (operation is not IInvocationOperation helperInvocation ||
                !SymbolEqualityComparer.Default.Equals(helperInvocation.TargetMethod.ContainingType, renderTreeBuilderType))
            {
                continue;
            }

            if (helperInvocation.TargetMethod.Name == "OpenComponent" &&
                helperInvocation.TargetMethod.IsGenericMethod &&
                helperInvocation.TargetMethod.TypeArguments.Length == 1 &&
                helperInvocation.TargetMethod.TypeArguments[0] is INamedTypeSymbol componentType &&
                SymbolEqualityComparer.Default.Equals(componentType.OriginalDefinition, virtualizeType))
            {
                createsVirtualize = true;
            }
            else if (helperInvocation.TargetMethod.Name == "AddComponentParameter" &&
                string.Equals(GetConstantStringArgument(helperInvocation, 1), "SpacerElement", StringComparison.Ordinal))
            {
                hasSpacerElement = true;
                if (helperInvocation.Arguments.Length > 2 &&
                    UnwrapConversion(helperInvocation.Arguments[2].Value) is IParameterReferenceOperation parameterReference)
                {
                    spacerParameterOrdinal = parameterReference.Parameter.Ordinal;
                }
            }
        }

        if (!createsVirtualize)
        {
            return false;
        }

        helperInfo = new VirtualizeHelperInfo(hasSpacerElement, spacerParameterOrdinal);
        return true;
    }

    private static IOperation UnwrapConversion(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static string? GetConstantStringArgument(IInvocationOperation invocation, int argumentIndex)
    {
        if (invocation.Arguments.Length <= argumentIndex)
        {
            return null;
        }

        var constantValue = UnwrapConversion(invocation.Arguments[argumentIndex].Value).ConstantValue;
        return constantValue.HasValue ? constantValue.Value as string : null;
    }

    private static void ReportInvalidSpacerElement(OperationBlockAnalysisContext context, RenderTreeFrame frame)
    {
        if (!frame.IsVirtualize ||
            frame.ParentElementName is null ||
            !AllowedSpacerElements.TryGetValue(frame.ParentElementName, out var allowedSpacerElements) ||
            (frame.HasSpacerElement && frame.SpacerElement is null))
        {
            return;
        }

        var spacerElement = frame.SpacerElement ?? "div";
        foreach (var allowedSpacerElement in allowedSpacerElements)
        {
            if (string.Equals(spacerElement, allowedSpacerElement, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        var allowedSpacerElementsMessage = allowedSpacerElements.Length == 1
            ? $"SpacerElement=\"{allowedSpacerElements[0]}\""
            : $"SpacerElement=\"{allowedSpacerElements[0]}\" or SpacerElement=\"{allowedSpacerElements[1]}\"";

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.VirtualizeSpacerElementIsInvalid,
            frame.Location,
            frame.ParentElementName,
            allowedSpacerElementsMessage));
    }

    private sealed class RenderTreeFrame
    {
        public bool IsComponent { get; set; }
        public bool IsVirtualize { get; set; }
        public bool HasSpacerElement { get; set; }
        public string? ElementName { get; set; }
        public string? ParentElementName { get; set; }
        public string? SpacerElement { get; set; }
        public Location? Location { get; set; }
    }

    private readonly struct VirtualizeHelperInfo
    {
        public VirtualizeHelperInfo(bool hasSpacerElement, int? spacerParameterOrdinal)
        {
            HasSpacerElement = hasSpacerElement;
            SpacerParameterOrdinal = spacerParameterOrdinal;
        }

        public bool HasSpacerElement { get; }
        public int? SpacerParameterOrdinal { get; }
    }
}