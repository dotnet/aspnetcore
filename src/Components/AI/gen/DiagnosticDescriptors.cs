// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.AI.SourceGenerators;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "BAIC001",
        title: "ToolBlock class must be partial",
        messageFormat: "ToolBlock class '{0}' must be declared as partial",
        category: "BlazorAIComponents",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WrongBaseClass = new(
        id: "BAIC002",
        title: "ToolBlock class must extend FunctionInvocationContentBlock",
        messageFormat: "ToolBlock class '{0}' must directly or indirectly extend FunctionInvocationContentBlock",
        category: "BlazorAIComponents",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IsAbstract = new(
        id: "BAIC003",
        title: "ToolBlock class must not be abstract",
        messageFormat: "ToolBlock class '{0}' must not be abstract",
        category: "BlazorAIComponents",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IsGeneric = new(
        id: "BAIC004",
        title: "ToolBlock class must not be generic",
        messageFormat: "ToolBlock class '{0}' must not be generic",
        category: "BlazorAIComponents",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EmptyToolName = new(
        id: "BAIC005",
        title: "ToolBlock has empty tool name",
        messageFormat: "ToolBlock '{0}' has an empty or null tool name",
        category: "BlazorAIComponents",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateArgumentKey = new(
        id: "BAIC006",
        title: "Duplicate argument key on ToolBlock",
        messageFormat: "Properties '{0}' and '{1}' on ToolBlock '{2}' map to the same argument key '{3}'",
        category: "BlazorAIComponents",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyNoSetter = new(
        id: "BAIC007",
        title: "ToolParameter property has no setter",
        messageFormat: "Property '{0}' on ToolBlock '{1}' has no setter — it will not be populated from arguments",
        category: "BlazorAIComponents",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateToolName = new(
        id: "BAIC008",
        title: "Duplicate tool name",
        messageFormat: "Duplicate tool name '{0}' — classes '{1}' and '{2}' both map to the same FunctionCallContent.Name",
        category: "BlazorAIComponents",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
