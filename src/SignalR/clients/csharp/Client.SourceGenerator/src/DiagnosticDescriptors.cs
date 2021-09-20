// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    internal static class DiagnosticDescriptors
    {
        // Ranges
        // SSG0000-0099: HubProxyGenerator
        // SSG0100-0199: CallbackRegistrationGenerator

        public static DiagnosticDescriptor HubProxyNonInterfaceGenericTypeArgument { get; } = new DiagnosticDescriptor(
            id: "SSG0000",
            title: "Non-interface generic type argument",
            messageFormat: "Only interfaces are accepted. '{0}' is not an interface.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyUnsupportedReturnType { get; } = new DiagnosticDescriptor(
            id: "SSG0001",
            title: "Unsupported return type",
            messageFormat: "'{0}' has a return type of '{1}' but only Task, ValueTask, Task<T> and ValueTask<T> are supported for source generation.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor TooManyGetProxyAttributedMethods { get; } = new DiagnosticDescriptor(
            id: "SSG0002",
            title: "Too many GetProxy attributed methods",
            messageFormat: "There can only be one GetProxy attributed method.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyGetProxyAttributedMethodBadAccessibility { get; } = new DiagnosticDescriptor(
            id: "SSG0003",
            title: "GetProxy attributed method has bad accessibility",
            messageFormat: "GetProxy attributed method may only have an accessibility of public, internal, protected, protected internal or private.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyGetProxyAttributedMethodIsNotPartial { get; } = new DiagnosticDescriptor(
            id: "SSG0004",
            title: "GetProxy attributed method has is not partial",
            messageFormat: "GetProxy attributed method must be partial.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyGetProxyAttributedMethodIsNotExtension { get; } = new DiagnosticDescriptor(
            id: "SSG0005",
            title: "GetProxy attributed method is not extension method",
            messageFormat: "GetProxy attributed method must be an extension method for HubConnection.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyGetProxyAttributedMethodTypeArgCountIsBad { get; } = new DiagnosticDescriptor(
            id: "SSG0006",
            title: "GetProxy attributed method has bad number of type arguments",
            messageFormat: "GetProxy attributed method must have exactly one type argument.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyGetProxyAttributedMethodTypeArgAndReturnTypeDoesNotMatch { get; } = new DiagnosticDescriptor(
            id: "SSG0007",
            title: "GetProxy attributed method type argument and return type does not match",
            messageFormat: "GetProxy attributed method must have the same type argument and return type",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyGetProxyAttributedMethodArgCountIsBad { get; } = new DiagnosticDescriptor(
            id: "SSG0008",
            title: "GetProxy attributed method has bad number of arguments",
            messageFormat: "GetProxy attributed method must have exactly one argument which must be of type HubConnection.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyGetProxyAttributedMethodArgIsNotHubConnection { get; } = new DiagnosticDescriptor(
            id: "SSG0009",
            title: "GetProxy attributed method has argument of wrong type",
            messageFormat: "GetProxy attributed method must have exactly one argument which must be of type HubConnection.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor CallbackRegistrationUnsupportedReturnType { get; } = new DiagnosticDescriptor(
            id: "SSG0100",
            title: "Unsupported return type",
            messageFormat: "'{0}' has a return type of '{1}' but only void and Task are supported for callback methods.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor TooManyRegisterCallbackProviderAttributedMethods { get; } = new DiagnosticDescriptor(
            id: "SSG0102",
            title: "Too many RegisterCallbackProvider attributed methods",
            messageFormat: "There can only be one RegisterCallbackProvider attributed method.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegisterCallbackProviderAttributedMethodBadAccessibility { get; } = new DiagnosticDescriptor(
            id: "SSG0103",
            title: "RegisterCallbackProvider attributed method has bad accessibility",
            messageFormat: "RegisterCallbackProvider attributed method may only have an accessibility of public, internal, protected, protected internal or private.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegisterCallbackProviderAttributedMethodIsNotPartial { get; } = new DiagnosticDescriptor(
            id: "SSG0104",
            title: "RegisterCallbackProvider attributed method has is not partial",
            messageFormat: "RegisterCallbackProvider attributed method must be partial.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegisterCallbackProviderAttributedMethodIsNotExtension { get; } = new DiagnosticDescriptor(
            id: "SSG0105",
            title: "RegisterCallbackProvider attributed method is not extension method",
            messageFormat: "RegisterCallbackProvider attributed method must be an extension method for HubConnection.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegisterCallbackProviderAttributedMethodTypeArgCountIsBad { get; } = new DiagnosticDescriptor(
            id: "SSG0106",
            title: "RegisterCallbackProvider attributed method has bad number of type arguments",
            messageFormat: "RegisterCallbackProvider attributed method must have exactly one type argument.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegisterCallbackProviderAttributedMethodTypeArgAndProviderTypeDoesNotMatch { get; } = new DiagnosticDescriptor(
            id: "SSG0107",
            title: "RegisterCallbackProvider attributed method type argument and return type does not match",
            messageFormat: "RegisterCallbackProvider attributed method must have the same type argument and return type",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegisterCallbackProviderAttributedMethodArgCountIsBad { get; } = new DiagnosticDescriptor(
            id: "SSG0108",
            title: "RegisterCallbackProvider attributed method has bad number of arguments",
            messageFormat: "RegisterCallbackProvider attributed method must have exactly two arguments.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegisterCallbackProviderAttributedMethodArgIsNotHubConnection { get; } = new DiagnosticDescriptor(
            id: "SSG0109",
            title: "RegisterCallbackProvider attributed method has first argument of wrong type",
            messageFormat: "RegisterCallbackProvider attributed method must have its first argument type be HubConnection.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RegisterCallbackProviderAttributedMethodHasBadReturnType { get; } = new DiagnosticDescriptor(
            id: "SSG0110",
            title: "RegisterCallbackProvider attributed method has wrong return type",
            messageFormat: "RegisterCallbackProvider attributed method must have a return type of IDisposable.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
