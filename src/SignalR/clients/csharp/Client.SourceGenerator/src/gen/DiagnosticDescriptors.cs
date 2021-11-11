// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator;

internal static class DiagnosticDescriptors
{
    // Ranges
    // SSG0000-0099: ServerHubProxyGenerator
    // SSG0100-0199: ClientHubGenerator

    public static DiagnosticDescriptor ServerHubProxyNonInterfaceGenericTypeArgument { get; } = new DiagnosticDescriptor(
        id: "SSG0000",
        title: "Non-interface generic type argument",
        messageFormat: "Only interfaces are accepted. '{0}' is not an interface.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ServerHubProxyUnsupportedReturnType { get; } = new DiagnosticDescriptor(
        id: "SSG0001",
        title: "Unsupported return type",
        messageFormat: "'{0}' has a return type of '{1}' but only Task, ValueTask, Task<T> and ValueTask<T> are supported for source generation.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TooManyServerHubProxyAttributedMethods { get; } = new DiagnosticDescriptor(
        id: "SSG0002",
        title: "Too many ServerHubProxy attributed methods",
        messageFormat: "There can only be one ServerHubProxy attributed method.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ServerHubProxyAttributedMethodBadAccessibility { get; } = new DiagnosticDescriptor(
        id: "SSG0003",
        title: "ServerHubProxy attributed method has bad accessibility",
        messageFormat: "ServerHubProxy attributed method may only have an accessibility of public, internal, protected, protected internal or private.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ServerHubProxyAttributedMethodIsNotPartial { get; } = new DiagnosticDescriptor(
        id: "SSG0004",
        title: "ServerHubProxy attributed method is not partial",
        messageFormat: "ServerHubProxy attributed method must be partial.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ServerHubProxyAttributedMethodIsNotExtension { get; } = new DiagnosticDescriptor(
        id: "SSG0005",
        title: "ServerHubProxy attributed method is not an extension method",
        messageFormat: "ServerHubProxy attributed method must be an extension method for HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ServerHubProxyAttributedMethodTypeArgCountIsBad { get; } = new DiagnosticDescriptor(
        id: "SSG0006",
        title: "ServerHubProxy attributed method has bad number of type arguments",
        messageFormat: "ServerHubProxy attributed method must have exactly one type argument.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ServerHubProxyAttributedMethodTypeArgAndReturnTypeDoesNotMatch { get; } = new DiagnosticDescriptor(
        id: "SSG0007",
        title: "ServerHubProxy attributed method type argument and return type does not match",
        messageFormat: "ServerHubProxy attributed method must have the same type argument and return type.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ServerHubProxyAttributedMethodArgCountIsBad { get; } = new DiagnosticDescriptor(
        id: "SSG0008",
        title: "ServerHubProxy attributed method has bad number of arguments",
        messageFormat: "ServerHubProxy attributed method must have exactly one argument which must be of type HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ServerHubProxyAttributedMethodArgIsNotHubConnection { get; } = new DiagnosticDescriptor(
        id: "SSG0009",
        title: "ServerHubProxy attributed method has argument of wrong type",
        messageFormat: "ServerHubProxy attributed method must have exactly one argument which must be of type HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ClientHub section

    public static DiagnosticDescriptor ClientHubUnsupportedReturnType { get; } = new DiagnosticDescriptor(
        id: "SSG0100",
        title: "Unsupported return type",
        messageFormat: "'{0}' has a return type of '{1}' but only void and Task are supported for callback methods.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TooManyClientHubAttributedMethods { get; } = new DiagnosticDescriptor(
        id: "SSG0102",
        title: "Too many ClientHub attributed methods",
        messageFormat: "There can only be one ClientHub attributed method.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClientHubAttributedMethodBadAccessibility { get; } = new DiagnosticDescriptor(
        id: "SSG0103",
        title: "ClientHub attributed method has bad accessibility",
        messageFormat: "ClientHub attributed method may only have an accessibility of public, internal, protected, protected internal or private.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClientHubAttributedMethodIsNotPartial { get; } = new DiagnosticDescriptor(
        id: "SSG0104",
        title: "ClientHub attributed method is not partial",
        messageFormat: "ClientHub attributed method must be partial.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClientHubAttributedMethodIsNotExtension { get; } = new DiagnosticDescriptor(
        id: "SSG0105",
        title: "ClientHub attributed method is not an extension method",
        messageFormat: "ClientHub attributed method must be an extension method for HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClientHubAttributedMethodTypeArgCountIsBad { get; } = new DiagnosticDescriptor(
        id: "SSG0106",
        title: "ClientHub attributed method has bad number of type arguments",
        messageFormat: "ClientHub attributed method must have exactly one type argument.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClientHubAttributedMethodTypeArgAndProviderTypeDoesNotMatch { get; } = new DiagnosticDescriptor(
        id: "SSG0107",
        title: "ClientHub attributed method type argument and return type does not match",
        messageFormat: "ClientHub attributed method must have the same type argument and return type.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClientHubAttributedMethodArgCountIsBad { get; } = new DiagnosticDescriptor(
        id: "SSG0108",
        title: "ClientHub attributed method has bad number of arguments",
        messageFormat: "ClientHub attributed method must have exactly two arguments.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClientHubAttributedMethodArgIsNotHubConnection { get; } = new DiagnosticDescriptor(
        id: "SSG0109",
        title: "ClientHub attributed method has first argument of wrong type",
        messageFormat: "ClientHub attributed method must have its first argument type be HubConnection.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClientHubAttributedMethodHasBadReturnType { get; } = new DiagnosticDescriptor(
        id: "SSG0110",
        title: "ClientHub attributed method has wrong return type",
        messageFormat: "ClientHub attributed method must have a return type of IDisposable.",
        category: "SignalR.Client.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
