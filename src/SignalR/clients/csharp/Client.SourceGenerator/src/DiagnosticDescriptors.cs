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

        public static DiagnosticDescriptor HubProxyMultipleClientToServerStreams { get; } = new DiagnosticDescriptor(
            id: "SSG0002",
            title: "Multiple client to server streams",
            messageFormat: "'{0}' has a signature with multiple client to server streams which is not supported for source generation.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyStreamTypeMismatch { get; } = new DiagnosticDescriptor(
            id: "SSG0003",
            title: "Stream type mismatch",
            messageFormat: "'{0}' has a signature with both ChannelReader and IAsyncEnumerable type streams.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyUnsupportedReturnTypeGeneral { get; } = new DiagnosticDescriptor(
            id: "SSG0004",
            title: "Unsupported return type",
            messageFormat: "'{0}' has a return type of '{1}' but only Task, ValueTask, Task<T> and ValueTask<T> are supported for source generation.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor HubProxyUnsupportedReturnTypeStream { get; } = new DiagnosticDescriptor(
            id: "SSG0005",
            title: "Unsupported return type (streaming call)",
            messageFormat: "'{0}' has a return type of '{1}' but only Task<T> and ValueTask<T> are supported for source generation of a client-to-server streaming method.",
            category: "SignalR.Client.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);


    }
}
