// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator;

internal static class DiagnosticDescriptors
{
    // Ranges
    // SSG0000-0099: HubServerProxyGenerator
    // SSG0100-0199: HubClientProxyGenerator

    public static DiagnosticDescriptor HubServerProxyNonInterfaceGenericTypeArgument { get; } = new DiagnosticDescriptor(
        id: "SSG0000",
        title: "Non-interface generic type argument",
        messageFormat: "Only interfaces are accepted. '{0}' is not an interface.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubServerProxyUnsupportedReturnType { get; } = new DiagnosticDescriptor(
        id: "SSG0001",
        title: "Unsupported return type",
        messageFormat: "'{0}' has a return type of '{1}' but only Task, ValueTask, Task<T> and ValueTask<T> are supported for source generation.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TooManyHubServerProxyAttributedMethods { get; } = new DiagnosticDescriptor(
        id: "SSG0002",
        title: "Too many HubServerProxy attributed methods",
        messageFormat: "There can only be one HubServerProxy attributed method.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubServerProxyAttributedMethodBadAccessibility { get; } = new DiagnosticDescriptor(
        id: "SSG0003",
        title: "HubServerProxy attributed method has bad accessibility",
        messageFormat: "HubServerProxy attributed method may only have an accessibility of public, internal, protected, protected internal or private.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubServerProxyAttributedMethodIsNotPartial { get; } = new DiagnosticDescriptor(
        id: "SSG0004",
        title: "HubServerProxy attributed method is not partial",
        messageFormat: "HubServerProxy attributed method must be partial.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubServerProxyAttributedMethodIsNotExtension { get; } = new DiagnosticDescriptor(
        id: "SSG0005",
        title: "HubServerProxy attributed method is not an extension method",
        messageFormat: "HubServerProxy attributed method must be an extension method for HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubServerProxyAttributedMethodTypeArgCountIsBad { get; } = new DiagnosticDescriptor(
        id: "SSG0006",
        title: "HubServerProxy attributed method has bad number of type arguments",
        messageFormat: "HubServerProxy attributed method must have exactly one type argument.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubServerProxyAttributedMethodTypeArgAndReturnTypeDoesNotMatch { get; } = new DiagnosticDescriptor(
        id: "SSG0007",
        title: "HubServerProxy attributed method type argument and return type does not match",
        messageFormat: "HubServerProxy attributed method must have the same type argument and return type.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubServerProxyAttributedMethodArgCountIsBad { get; } = new DiagnosticDescriptor(
        id: "SSG0008",
        title: "HubServerProxy attributed method has bad number of arguments",
        messageFormat: "HubServerProxy attributed method must have exactly one argument which must be of type HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubServerProxyAttributedMethodArgIsNotHubConnection { get; } = new DiagnosticDescriptor(
        id: "SSG0009",
        title: "HubServerProxy attributed method has argument of wrong type",
        messageFormat: "HubServerProxy attributed method must have exactly one argument which must be of type HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // HubClientProxy section

    public static DiagnosticDescriptor HubClientProxyUnsupportedReturnType { get; } = new DiagnosticDescriptor(
        id: "SSG0100",
        title: "Unsupported return type",
        messageFormat: "'{0}' has a return type of '{1}' but only void and Task are supported for callback methods.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TooManyHubClientProxyAttributedMethods { get; } = new DiagnosticDescriptor(
        id: "SSG0102",
        title: "Too many HubClientProxy attributed methods",
        messageFormat: "There can only be one HubClientProxy attributed method.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubClientProxyAttributedMethodBadAccessibility { get; } = new DiagnosticDescriptor(
        id: "SSG0103",
        title: "HubClientProxy attributed method has bad accessibility",
        messageFormat: "HubClientProxy attributed method may only have an accessibility of public, internal, protected, protected internal or private.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubClientProxyAttributedMethodIsNotPartial { get; } = new DiagnosticDescriptor(
        id: "SSG0104",
        title: "HubClientProxy attributed method is not partial",
        messageFormat: "HubClientProxy attributed method must be partial.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubClientProxyAttributedMethodIsNotExtension { get; } = new DiagnosticDescriptor(
        id: "SSG0105",
        title: "HubClientProxy attributed method is not an extension method",
        messageFormat: "HubClientProxy attributed method must be an extension method for HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubClientProxyAttributedMethodTypeArgCountIsBad { get; } = new DiagnosticDescriptor(
        id: "SSG0106",
        title: "HubClientProxy attributed method has bad number of type arguments",
        messageFormat: "HubClientProxy attributed method must have exactly one type argument.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubClientProxyAttributedMethodTypeArgAndProviderTypeDoesNotMatch { get; } = new DiagnosticDescriptor(
        id: "SSG0107",
        title: "HubClientProxy attributed method type argument and return type does not match",
        messageFormat: "HubClientProxy attributed method must have the same type argument and return type.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubClientProxyAttributedMethodArgCountIsBad { get; } = new DiagnosticDescriptor(
        id: "SSG0108",
        title: "HubClientProxy attributed method has bad number of arguments",
        messageFormat: "HubClientProxy attributed method must have exactly two arguments.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubClientProxyAttributedMethodArgIsNotHubConnection { get; } = new DiagnosticDescriptor(
        id: "SSG0109",
        title: "HubClientProxy attributed method has first argument of wrong type",
        messageFormat: "HubClientProxy attributed method must have its first argument type be HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor HubClientProxyAttributedMethodHasBadReturnType { get; } = new DiagnosticDescriptor(
        id: "SSG0110",
        title: "HubClientProxy attributed method has wrong return type",
        messageFormat: "HubClientProxy attributed method must have a return type of IDisposable.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
