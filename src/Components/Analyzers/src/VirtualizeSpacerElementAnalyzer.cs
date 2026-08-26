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
                foreach (var operationBlock in blockContext.OperationBlocks)
                {
                    var renderTreeStacks = new Dictionary<ISymbol, Stack<RenderTreeFrame>>(SymbolEqualityComparer.Default);

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
                                renderTreeStacks,
                                virtualizeType,
                                renderTreeBuilderType);
                            continue;
                        }

                        if (GetReferencedSymbol(invocation.Instance) is not { } builderSymbol)
                        {
                            continue;
                        }

                        if (!renderTreeStacks.TryGetValue(builderSymbol, out var renderTreeStack))
                        {
                            renderTreeStack = new Stack<RenderTreeFrame>();
                            renderTreeStacks.Add(builderSymbol, renderTreeStack);
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

                            case "AddAttribute":
                            case "AddComponentParameter":
                                if (renderTreeStack.Count > 0 &&
                                    renderTreeStack.Peek() is { IsVirtualize: true } virtualizeFrame &&
                                    string.Equals(GetConstantStringArgument(invocation, 1), "SpacerElement", StringComparison.Ordinal))
                                {
                                    virtualizeFrame.HasSpacerElement = true;
                                    virtualizeFrame.SpacerElement = GetConstantStringArgument(invocation, 2);
                                }
                                break;

                            case "AddMultipleAttributes":
                                if (renderTreeStack.Count > 0 &&
                                    renderTreeStack.Peek() is { IsVirtualize: true } splattedVirtualizeFrame &&
                                    !IsConstantNull(GetArgumentValue(invocation, 1)))
                                {
                                    splattedVirtualizeFrame.HasSpacerElement = true;
                                    splattedVirtualizeFrame.SpacerElement = null;
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
        Dictionary<ISymbol, Stack<RenderTreeFrame>> renderTreeStacks,
        INamedTypeSymbol virtualizeType,
        INamedTypeSymbol renderTreeBuilderType)
    {
        if (!TryGetVirtualizeHelperInfo(
                context.Compilation,
                invocation.TargetMethod,
                virtualizeType,
                renderTreeBuilderType,
                context.CancellationToken,
                out var helperInfo) ||
            GetArgumentValue(invocation, helperInfo.BuilderParameterOrdinal) is not { } builderArgument ||
            GetReferencedSymbol(builderArgument) is not { } builderSymbol ||
            !renderTreeStacks.TryGetValue(builderSymbol, out var renderTreeStack) ||
            renderTreeStack.Count == 0 ||
            renderTreeStack.Peek().IsComponent ||
            renderTreeStack.Peek().ElementName is not { } parentElementName ||
            !AllowedSpacerElements.ContainsKey(parentElementName))
        {
            return;
        }

        var hasSpacerElement = false;
        string? spacerElement = null;
        foreach (var attributeUpdate in helperInfo.AttributeUpdates)
        {
            IOperation? value = null;
            if (attributeUpdate.ParameterOrdinal is { } parameterOrdinal)
            {
                value = GetArgumentValue(invocation, parameterOrdinal);
            }

            var constantValue = value is not null
                ? UnwrapConversion(value).ConstantValue
                : attributeUpdate.ConstantValue;

            if (attributeUpdate.IsSplat)
            {
                if (constantValue.HasValue && constantValue.Value is null)
                {
                    continue;
                }

                hasSpacerElement = true;
                spacerElement = null;
                continue;
            }

            hasSpacerElement = true;
            if (!constantValue.HasValue || constantValue.Value is not string valueString)
            {
                return;
            }

            spacerElement = valueString;
        }

        ReportInvalidSpacerElement(context, new RenderTreeFrame
        {
            IsVirtualize = true,
            HasSpacerElement = hasSpacerElement,
            ParentElementName = parentElementName,
            SpacerElement = spacerElement,
            Location = GetDiagnosticLocation(invocation),
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

        IParameterSymbol? builderParameter = null;
        foreach (var parameter in method.OriginalDefinition.Parameters)
        {
            if (SymbolEqualityComparer.Default.Equals(parameter.Type, renderTreeBuilderType))
            {
                builderParameter = parameter;
                break;
            }
        }

        if (builderParameter is null)
        {
            return false;
        }

        var createsVirtualize = false;
        var attributeUpdates = ImmutableArray.CreateBuilder<VirtualizeAttributeUpdate>();
        var renderTreeDepth = 0;

        foreach (var operation in methodBody.DescendantsAndSelf())
        {
            if (operation is not IInvocationOperation helperInvocation ||
                !SymbolEqualityComparer.Default.Equals(helperInvocation.TargetMethod.ContainingType, renderTreeBuilderType) ||
                GetReferencedSymbol(helperInvocation.Instance) is not { } helperBuilderSymbol ||
                !SymbolEqualityComparer.Default.Equals(helperBuilderSymbol, builderParameter))
            {
                continue;
            }

            switch (helperInvocation.TargetMethod.Name)
            {
                case "OpenElement":
                    renderTreeDepth++;
                    break;

                case "OpenComponent":
                    if (renderTreeDepth == 0 &&
                        helperInvocation.TargetMethod.IsGenericMethod &&
                        helperInvocation.TargetMethod.TypeArguments.Length == 1 &&
                        helperInvocation.TargetMethod.TypeArguments[0] is INamedTypeSymbol componentType &&
                        SymbolEqualityComparer.Default.Equals(componentType.OriginalDefinition, virtualizeType))
                    {
                        createsVirtualize = true;
                    }

                    renderTreeDepth++;
                    break;

                case "AddAttribute":
                case "AddComponentParameter":
                    if (createsVirtualize &&
                        renderTreeDepth == 1 &&
                        string.Equals(GetConstantStringArgument(helperInvocation, 1), "SpacerElement", StringComparison.Ordinal))
                    {
                        if (GetArgumentValue(helperInvocation, 2) is { } spacerValue)
                        {
                            attributeUpdates.Add(VirtualizeAttributeUpdate.CreateSpacerElement(spacerValue));
                        }
                    }
                    break;

                case "AddMultipleAttributes":
                    if (createsVirtualize &&
                        renderTreeDepth == 1 &&
                        GetArgumentValue(helperInvocation, 1) is { } attributesValue &&
                        !IsConstantNull(attributesValue))
                    {
                        attributeUpdates.Add(VirtualizeAttributeUpdate.CreateSplat(attributesValue));
                    }
                    break;

                case "CloseComponent":
                case "CloseElement":
                    renderTreeDepth--;
                    break;
            }
        }

        if (!createsVirtualize)
        {
            return false;
        }

        helperInfo = new VirtualizeHelperInfo(builderParameter.Ordinal, attributeUpdates.ToImmutable());
        return true;
    }

    private static Location GetDiagnosticLocation(IInvocationOperation invocation)
    {
        foreach (var argument in invocation.Arguments)
        {
            var location = UnwrapConversion(argument.Value).Syntax.GetLocation();
            if (location.GetMappedLineSpan().HasMappedPath)
            {
                return location;
            }
        }

        return invocation.Syntax.GetLocation();
    }

    private static IOperation? GetArgumentValue(IInvocationOperation invocation, int parameterOrdinal)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == parameterOrdinal)
            {
                return argument.Value;
            }
        }

        return null;
    }

    private static ISymbol? GetReferencedSymbol(IOperation? operation)
    {
        if (operation is null)
        {
            return null;
        }

        return UnwrapConversion(operation) switch
        {
            IParameterReferenceOperation parameterReference => parameterReference.Parameter,
            ILocalReferenceOperation localReference => localReference.Local,
            IFieldReferenceOperation fieldReference => fieldReference.Field,
            IPropertyReferenceOperation propertyReference => propertyReference.Property,
            _ => null,
        };
    }

    private static bool IsConstantNull(IOperation? operation)
    {
        if (operation is null)
        {
            return false;
        }

        var constantValue = UnwrapConversion(operation).ConstantValue;
        return constantValue.HasValue && constantValue.Value is null;
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
            spacerElement,
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
        public VirtualizeHelperInfo(int builderParameterOrdinal, ImmutableArray<VirtualizeAttributeUpdate> attributeUpdates)
        {
            BuilderParameterOrdinal = builderParameterOrdinal;
            AttributeUpdates = attributeUpdates;
        }

        public int BuilderParameterOrdinal { get; }
        public ImmutableArray<VirtualizeAttributeUpdate> AttributeUpdates { get; }
    }

    private readonly struct VirtualizeAttributeUpdate
    {
        private VirtualizeAttributeUpdate(bool isSplat, int? parameterOrdinal, Optional<object?> constantValue)
        {
            IsSplat = isSplat;
            ParameterOrdinal = parameterOrdinal;
            ConstantValue = constantValue;
        }

        public bool IsSplat { get; }
        public int? ParameterOrdinal { get; }
        public Optional<object?> ConstantValue { get; }

        public static VirtualizeAttributeUpdate CreateSpacerElement(IOperation value) => Create(value, isSplat: false);

        public static VirtualizeAttributeUpdate CreateSplat(IOperation value) => Create(value, isSplat: true);

        private static VirtualizeAttributeUpdate Create(IOperation value, bool isSplat)
        {
            value = UnwrapConversion(value);
            return value is IParameterReferenceOperation parameterReference
                ? new VirtualizeAttributeUpdate(isSplat, parameterReference.Parameter.Ordinal, default)
                : new VirtualizeAttributeUpdate(isSplat, null, value.ConstantValue);
        }
    }
}