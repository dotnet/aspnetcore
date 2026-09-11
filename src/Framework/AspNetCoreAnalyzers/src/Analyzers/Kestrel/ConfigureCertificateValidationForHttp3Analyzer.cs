// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.Kestrel;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConfigureCertificateValidationForHttp3Analyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ConfigureCertificateValidationForHttp3];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            var sslServerAuthenticationOptions = context.Compilation.GetTypeByMetadataName("System.Net.Security.SslServerAuthenticationOptions");
            var sslStreamCertificateContext = context.Compilation.GetTypeByMetadataName("System.Net.Security.SslStreamCertificateContext");
            var sslCertificateTrust = context.Compilation.GetTypeByMetadataName("System.Net.Security.SslCertificateTrust");
            var tlsHandshakeCallbackOptions = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Server.Kestrel.Https.TlsHandshakeCallbackOptions");
            var listenOptions = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions");
            var httpProtocols = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols");

            if (sslServerAuthenticationOptions is null ||
                sslStreamCertificateContext is null ||
                sslCertificateTrust is null ||
                tlsHandshakeCallbackOptions is null ||
                listenOptions is null ||
                httpProtocols is null)
            {
                return;
            }

            var http3 = httpProtocols.GetMembers("Http3").OfType<IFieldSymbol>().SingleOrDefault()?.ConstantValue;
            if (http3 is null)
            {
                return;
            }

            context.RegisterOperationAction(
                context => AnalyzeObjectCreation(
                    context,
                    sslServerAuthenticationOptions,
                    sslStreamCertificateContext,
                    sslCertificateTrust,
                    tlsHandshakeCallbackOptions,
                    listenOptions,
                    Convert.ToInt64(http3, CultureInfo.InvariantCulture)),
                OperationKind.ObjectCreation);
        });
    }

    private static void AnalyzeObjectCreation(
        OperationAnalysisContext context,
        INamedTypeSymbol sslServerAuthenticationOptions,
        INamedTypeSymbol sslStreamCertificateContext,
        INamedTypeSymbol sslCertificateTrust,
        INamedTypeSymbol tlsHandshakeCallbackOptions,
        INamedTypeSymbol listenOptions,
        long http3)
    {
        var objectCreation = (IObjectCreationOperation)context.Operation;
        if (!SymbolEqualityComparer.Default.Equals(objectCreation.Type, sslServerAuthenticationOptions) ||
            objectCreation.Initializer is null)
        {
            return;
        }

        ISimpleAssignmentOperation? serverCertificateContextAssignment = null;
        var clientCertificateRequired = false;
        var hasCustomValidation = false;

        foreach (var initializer in objectCreation.Initializer.Initializers.OfType<ISimpleAssignmentOperation>())
        {
            if (initializer.Target is not IPropertyReferenceOperation propertyReference ||
                !SymbolEqualityComparer.Default.Equals(propertyReference.Property.ContainingType, sslServerAuthenticationOptions))
            {
                continue;
            }

            switch (propertyReference.Property.Name)
            {
                case "ClientCertificateRequired":
                    clientCertificateRequired = initializer.Value.ConstantValue is { HasValue: true, Value: true };
                    break;
                case "RemoteCertificateValidationCallback":
                case "CertificateChainPolicy":
                    hasCustomValidation |= !IsNull(initializer.Value);
                    break;
                case "ServerCertificateContext" when UsesCertificateTrust(initializer.Value, sslStreamCertificateContext, sslCertificateTrust):
                    serverCertificateContextAssignment = initializer;
                    break;
            }
        }

        if (!clientCertificateRequired ||
            hasCustomValidation ||
            serverCertificateContextAssignment is null ||
            !TryGetKestrelUseHttpsInvocation(objectCreation, tlsHandshakeCallbackOptions, out var useHttpsInvocation) ||
            !IsHttp3EnabledInContainingScope(useHttpsInvocation, listenOptions, http3))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ConfigureCertificateValidationForHttp3,
            serverCertificateContextAssignment.Target.Syntax.GetLocation()));
    }

    private static bool UsesCertificateTrust(
        IOperation value,
        INamedTypeSymbol sslStreamCertificateContext,
        INamedTypeSymbol sslCertificateTrust)
    {
        value = UnwrapConversion(value);
        if (value is not IInvocationOperation
            {
                TargetMethod.Name: "Create"
            } invocation ||
            !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, sslStreamCertificateContext))
        {
            return false;
        }

        var trustArgument = invocation.Arguments.FirstOrDefault(argument =>
            SymbolEqualityComparer.Default.Equals(argument.Parameter?.Type, sslCertificateTrust));

        return trustArgument is not null &&
            UnwrapConversion(trustArgument.Value) is IInvocationOperation trustCreation &&
            SymbolEqualityComparer.Default.Equals(trustCreation.Type, sslCertificateTrust);
    }

    private static bool TryGetKestrelUseHttpsInvocation(
        IObjectCreationOperation sslOptionsCreation,
        INamedTypeSymbol tlsHandshakeCallbackOptions,
        out IInvocationOperation useHttpsInvocation)
    {
        useHttpsInvocation = null!;

        var callback = GetAncestors(sslOptionsCreation).OfType<IAnonymousFunctionOperation>().FirstOrDefault();
        if (callback is null)
        {
            return false;
        }

        var onConnectionAssignment = GetAncestors(callback).OfType<ISimpleAssignmentOperation>().FirstOrDefault();
        if (onConnectionAssignment?.Target is not IPropertyReferenceOperation
            {
                Property.Name: "OnConnection",
                Property.ContainingType: var containingType
            } ||
            !SymbolEqualityComparer.Default.Equals(containingType, tlsHandshakeCallbackOptions))
        {
            return false;
        }

        var callbackOptionsCreation = GetAncestors(onConnectionAssignment).OfType<IObjectCreationOperation>().FirstOrDefault();
        var invocation = callbackOptionsCreation is null
            ? null
            : GetAncestors(callbackOptionsCreation).OfType<IInvocationOperation>().FirstOrDefault();

        if (invocation is null ||
            invocation.TargetMethod.Name != "UseHttps" ||
            !invocation.Arguments.Any(argument =>
                SymbolEqualityComparer.Default.Equals(argument.Parameter?.Type, tlsHandshakeCallbackOptions)))
        {
            return false;
        }

        useHttpsInvocation = invocation;
        return true;
    }

    private static bool IsHttp3EnabledInContainingScope(
        IInvocationOperation useHttpsInvocation,
        INamedTypeSymbol listenOptions,
        long http3)
    {
        var receiver = useHttpsInvocation.Instance ??
            useHttpsInvocation.Arguments.FirstOrDefault(argument =>
                SymbolEqualityComparer.Default.Equals(argument.Parameter?.Type, listenOptions))?.Value;
        var receiverSymbol = GetReferencedSymbol(receiver);
        if (receiverSymbol is null)
        {
            return false;
        }

        var containingBlock = GetAncestors(useHttpsInvocation).OfType<IBlockOperation>().FirstOrDefault();
        if (containingBlock is null)
        {
            return false;
        }

        foreach (var assignment in containingBlock.Descendants().OfType<ISimpleAssignmentOperation>())
        {
            if (assignment.Target is not IPropertyReferenceOperation
                {
                    Property.Name: "Protocols",
                    Instance: var instance
                } propertyReference ||
                !SymbolEqualityComparer.Default.Equals(propertyReference.Property.ContainingType, listenOptions) ||
                !SymbolEqualityComparer.Default.Equals(GetReferencedSymbol(instance), receiverSymbol) ||
                assignment.Parent?.Parent != containingBlock ||
                assignment.Value.ConstantValue is not { HasValue: true, Value: var value } ||
                (Convert.ToInt64(value, CultureInfo.InvariantCulture) & http3) == 0)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static ISymbol? GetReferencedSymbol(IOperation? operation)
        => operation is null ? null : UnwrapConversion(operation) switch
        {
            ILocalReferenceOperation local => local.Local,
            IParameterReferenceOperation parameter => parameter.Parameter,
            IFieldReferenceOperation field => field.Field,
            IPropertyReferenceOperation property => property.Property,
            _ => null,
        };

    private static bool IsNull(IOperation operation)
        => UnwrapConversion(operation).ConstantValue is { HasValue: true, Value: null };

    private static IOperation UnwrapConversion(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static System.Collections.Generic.IEnumerable<IOperation> GetAncestors(IOperation operation)
    {
        for (var current = operation.Parent; current is not null; current = current.Parent)
        {
            yield return current;
        }
    }
}
