using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    internal static class DiagnosticDescriptors
    {
        // Ranges
        // SSG0000-0099: HubProxyGenerator
        // SSG0100-0199: CallbackRegistrationGenerator

        public static DiagnosticDescriptor HubProxyNonInterfaceGenericTypeArgument { get; } = new DiagnosticDescriptor(
            id: "SSG0001",
            title: "Non-interface generic type argument",
            messageFormat: "'GetProxy<THub>' only accepts interfaces. '{0}' is not an interface.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyUnsupportedReturnTypeGeneral { get; } = new DiagnosticDescriptor(
            id: "SSG0002",
            title: "Unsupported return type",
            messageFormat: "'{0}' has a return type of '{1}' but only Task, ValueTask, Task<T> and ValueTask<T> are supported for source generation.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyUnsupportedReturnTypeStream { get; } = new DiagnosticDescriptor(
            id: "SSG0003",
            title: "Unsupported return type (streaming call)",
            messageFormat: "'{0}' has a return type of '{1}' but only Task and ValueTask are supported for source generation of a client-to-server streaming method.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
