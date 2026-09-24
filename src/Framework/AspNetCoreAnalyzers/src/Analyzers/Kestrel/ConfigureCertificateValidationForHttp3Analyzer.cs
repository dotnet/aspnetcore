// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var listenOptionsHttpsExtensions = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Hosting.ListenOptionsHttpsExtensions");
            var httpProtocols = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols");
            var x509ChainPolicy = context.Compilation.GetTypeByMetadataName("System.Security.Cryptography.X509Certificates.X509ChainPolicy");
            var x509ChainTrustMode = context.Compilation.GetTypeByMetadataName("System.Security.Cryptography.X509Certificates.X509ChainTrustMode");

            if (sslServerAuthenticationOptions is null ||
                sslStreamCertificateContext is null ||
                sslCertificateTrust is null ||
                tlsHandshakeCallbackOptions is null ||
                listenOptions is null ||
                listenOptionsHttpsExtensions is null ||
                httpProtocols is null ||
                x509ChainPolicy is null ||
                x509ChainTrustMode is null)
            {
                return;
            }

            var useHttpsWithCallbackOptions = listenOptionsHttpsExtensions.GetMembers("UseHttps")
                .OfType<IMethodSymbol>()
                .SingleOrDefault(method =>
                    method.Parameters.Length is 2 &&
                    SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, listenOptions) &&
                    SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, tlsHandshakeCallbackOptions));
            var http3 = httpProtocols.GetMembers("Http3").OfType<IFieldSymbol>().SingleOrDefault()?.ConstantValue;
            var customRootTrust = x509ChainTrustMode.GetMembers("CustomRootTrust").OfType<IFieldSymbol>().SingleOrDefault()?.ConstantValue;
            if (useHttpsWithCallbackOptions is null || http3 is null || customRootTrust is null)
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
                    useHttpsWithCallbackOptions,
                    x509ChainPolicy,
                    Convert.ToInt64(http3, CultureInfo.InvariantCulture),
                    Convert.ToInt64(customRootTrust, CultureInfo.InvariantCulture)),
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
        IMethodSymbol useHttpsWithCallbackOptions,
        INamedTypeSymbol x509ChainPolicy,
        long http3,
        long customRootTrust)
    {
        var objectCreation = (IObjectCreationOperation)context.Operation;
        if (!SymbolEqualityComparer.Default.Equals(objectCreation.Type, sslServerAuthenticationOptions) ||
            objectCreation.Initializer is null)
        {
            return;
        }

        ISimpleAssignmentOperation? serverCertificateContextAssignment = null;
        var clientCertificateRequired = false;
        var hasEffectiveValidation = false;

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
                    hasEffectiveValidation |= HasEffectiveValidationCallback(initializer.Value);
                    break;
                case "CertificateChainPolicy":
                    hasEffectiveValidation |= HasEffectiveChainPolicy(initializer.Value, x509ChainPolicy, customRootTrust);
                    break;
                case "ServerCertificateContext" when UsesCertificateTrust(initializer.Value, sslStreamCertificateContext, sslCertificateTrust):
                    serverCertificateContextAssignment = initializer;
                    break;
            }
        }

        if (!clientCertificateRequired ||
            hasEffectiveValidation ||
            serverCertificateContextAssignment is null ||
            !TryGetKestrelUseHttpsInvocation(objectCreation, tlsHandshakeCallbackOptions, useHttpsWithCallbackOptions, out var useHttpsInvocation) ||
            !IsHttp3EnabledInContainingScope(useHttpsInvocation, listenOptions, http3))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ConfigureCertificateValidationForHttp3,
            serverCertificateContextAssignment.Target.Syntax.GetLocation()));
    }

    private static bool HasEffectiveValidationCallback(IOperation value)
    {
        value = UnwrapConversion(value);
        if (IsNull(value))
        {
            return false;
        }

        var callback = value switch
        {
            IAnonymousFunctionOperation anonymousFunction => anonymousFunction,
            IDelegateCreationOperation { Target: IAnonymousFunctionOperation anonymousFunction } => anonymousFunction,
            _ => null,
        };
        if (callback is null)
        {
            // Method groups, delegate references, and other opaque values might perform validation.
            return true;
        }

        // Only a visibly unconditional accept-all callback is ineffective. Any other callback
        // shape remains outside this diagnostic rather than being audited for correctness.
        if (callback.Syntax is LambdaExpressionSyntax
            {
                Body: LiteralExpressionSyntax { Token.Value: true }
            })
        {
            return false;
        }

        var block = callback.Syntax switch
        {
            LambdaExpressionSyntax { Body: BlockSyntax lambdaBlock } => lambdaBlock,
            AnonymousMethodExpressionSyntax { Block: var anonymousMethodBlock } => anonymousMethodBlock,
            _ => null,
        };
        if (block is null)
        {
            return true;
        }

        static bool DescendIntoCallbackNode(SyntaxNode node)
            => node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax;

        if (block.DescendantNodes(DescendIntoCallbackNode)
            .Any(node => node is ThrowStatementSyntax or ThrowExpressionSyntax))
        {
            return true;
        }

        var returns = block.DescendantNodes(DescendIntoCallbackNode)
            .OfType<ReturnStatementSyntax>()
            .ToArray();

        return returns.Length is 0 ||
            returns.Any(returnStatement => returnStatement.Expression is not LiteralExpressionSyntax { Token.Value: true });
    }

    private static bool HasEffectiveChainPolicy(
        IOperation value,
        INamedTypeSymbol x509ChainPolicy,
        long customRootTrust)
    {
        value = UnwrapConversion(value);
        if (IsNull(value))
        {
            return false;
        }

        if (value is not IObjectCreationOperation policyCreation ||
            !SymbolEqualityComparer.Default.Equals(policyCreation.Type, x509ChainPolicy))
        {
            // A policy configured elsewhere might use custom root trust. Avoid claiming it is unsafe.
            return true;
        }

        if (policyCreation.Initializer is null)
        {
            return false;
        }

        var hasCustomRootTrust = false;
        var hasPopulatedCustomTrustStore = false;
        var hasUnknownTrustMode = false;

        foreach (var initializer in policyCreation.Initializer.Initializers)
        {
            if (initializer is ISimpleAssignmentOperation
                {
                    Target: IPropertyReferenceOperation { Property.Name: "TrustMode" }
                } trustModeAssignment &&
                UnwrapConversion(trustModeAssignment.Value).ConstantValue is { HasValue: true, Value: var trustMode })
            {
                hasCustomRootTrust = Convert.ToInt64(trustMode, CultureInfo.InvariantCulture) == customRootTrust;
            }
            else if (initializer is ISimpleAssignmentOperation
            {
                Target: IPropertyReferenceOperation { Property.Name: "TrustMode" }
            })
            {
                hasUnknownTrustMode = true;
            }
            else if (initializer is IMemberInitializerOperation
            {
                InitializedMember: IPropertyReferenceOperation { Property.Name: "CustomTrustStore" },
                Initializer: var customTrustStoreInitializer
            } &&
                customTrustStoreInitializer.DescendantsAndSelf().OfType<IInvocationOperation>().Any(
                    invocation => invocation.TargetMethod.Name == "Add" &&
                        invocation.Arguments.Length is 1 &&
                        !IsNull(invocation.Arguments[0].Value)))
            {
                hasPopulatedCustomTrustStore = true;
            }
        }

        return hasUnknownTrustMode || (hasCustomRootTrust && hasPopulatedCustomTrustStore);
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
            SymbolEqualityComparer.Default.Equals(trustCreation.TargetMethod.ContainingType, sslCertificateTrust);
    }

    private static bool TryGetKestrelUseHttpsInvocation(
        IObjectCreationOperation sslOptionsCreation,
        INamedTypeSymbol tlsHandshakeCallbackOptions,
        IMethodSymbol useHttpsWithCallbackOptions,
        out IInvocationOperation useHttpsInvocation)
    {
        useHttpsInvocation = null!;

        var callback = GetAncestors(sslOptionsCreation).OfType<IAnonymousFunctionOperation>().FirstOrDefault();
        if (callback is null || !IsDirectlyReturned(sslOptionsCreation, callback))
        {
            return false;
        }

        var onConnectionAssignment = GetAncestors(callback).OfType<ISimpleAssignmentOperation>().FirstOrDefault();
        if (onConnectionAssignment?.Target is not IPropertyReferenceOperation
            {
                Property.Name: "OnConnection",
                Property.ContainingType: var containingType
            } ||
            !SymbolEqualityComparer.Default.Equals(containingType, tlsHandshakeCallbackOptions) ||
            onConnectionAssignment.Value.DescendantsAndSelf().OfType<IAnonymousFunctionOperation>().FirstOrDefault() != callback)
        {
            return false;
        }

        var callbackOptionsCreation = GetAncestors(onConnectionAssignment).OfType<IObjectCreationOperation>().FirstOrDefault();
        var invocation = callbackOptionsCreation is null
            ? null
            : GetAncestors(callbackOptionsCreation).OfType<IInvocationOperation>().FirstOrDefault();

        var targetMethod = invocation?.TargetMethod.ReducedFrom ?? invocation?.TargetMethod;
        if (invocation is null ||
            !SymbolEqualityComparer.Default.Equals(targetMethod?.OriginalDefinition, useHttpsWithCallbackOptions))
        {
            return false;
        }

        useHttpsInvocation = invocation;
        return true;
    }

    private static bool IsDirectlyReturned(
        IObjectCreationOperation sslOptionsCreation,
        IAnonymousFunctionOperation callback)
    {
        var returnOperation = GetAncestors(sslOptionsCreation).OfType<IReturnOperation>().FirstOrDefault();
        if (returnOperation?.ReturnedValue is null ||
            GetAncestors(returnOperation).OfType<IAnonymousFunctionOperation>().FirstOrDefault() != callback)
        {
            return false;
        }

        var returnedValue = UnwrapConversion(returnOperation.ReturnedValue);
        if (returnedValue == sslOptionsCreation)
        {
            return true;
        }

        if (returnedValue is IInvocationOperation
            {
                TargetMethod:
                {
                    Name: "FromResult",
                    ContainingType:
                    {
                        Name: "ValueTask",
                        ContainingNamespace:
                        {
                            Name: "Tasks",
                            ContainingNamespace:
                            {
                                Name: "Threading",
                                ContainingNamespace:
                                {
                                    Name: "System",
                                    ContainingNamespace.IsGlobalNamespace: true
                                }
                            }
                        }
                    }
                }
            } invocation &&
            invocation.Arguments.Length is 1)
        {
            return UnwrapConversion(invocation.Arguments[0].Value) == sslOptionsCreation;
        }

        if (returnedValue is IObjectCreationOperation
            {
                Type:
                {
                    Name: "ValueTask",
                    ContainingNamespace:
                    {
                        Name: "Tasks",
                        ContainingNamespace:
                        {
                            Name: "Threading",
                            ContainingNamespace:
                            {
                                Name: "System",
                                ContainingNamespace.IsGlobalNamespace: true
                            }
                        }
                    }
                }
            } constructor &&
            constructor.Arguments.Length is 1)
        {
            return UnwrapConversion(constructor.Arguments[0].Value) == sslOptionsCreation;
        }

        return false;
    }

    private static bool IsHttp3EnabledInContainingScope(
        IInvocationOperation useHttpsInvocation,
        INamedTypeSymbol listenOptions,
        long http3)
    {
        var receiver = useHttpsInvocation.Instance ??
            useHttpsInvocation.Arguments.FirstOrDefault(argument =>
                SymbolEqualityComparer.Default.Equals(argument.Parameter?.Type, listenOptions))?.Value;
        if (receiver is null)
        {
            return false;
        }

        var containingBlock = GetAncestors(useHttpsInvocation).OfType<IBlockOperation>().FirstOrDefault();
        if (containingBlock is null)
        {
            return false;
        }

        ISimpleAssignmentOperation? finalAssignment = null;
        foreach (var assignment in containingBlock.Descendants().OfType<IAssignmentOperation>())
        {
            if (assignment.Target is not IPropertyReferenceOperation
                {
                    Property.Name: "Protocols",
                    Instance: var instance
                } propertyReference ||
                !SymbolEqualityComparer.Default.Equals(propertyReference.Property.ContainingType, listenOptions) ||
                !AreSameReceiver(instance, receiver))
            {
                continue;
            }

            if (assignment is not ISimpleAssignmentOperation simpleAssignment ||
                assignment.Parent?.Parent != containingBlock ||
                assignment.Value.ConstantValue is not { HasValue: true })
            {
                // Conditional, compound, and nonconstant protocol configuration is unresolved.
                return false;
            }

            finalAssignment = simpleAssignment;
        }

        return finalAssignment?.Value.ConstantValue is { HasValue: true, Value: var value } &&
            (Convert.ToInt64(value, CultureInfo.InvariantCulture) & http3) != 0;
    }

    private static bool AreSameReceiver(IOperation? left, IOperation? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        left = UnwrapConversion(left);
        right = UnwrapConversion(right);

        return (left, right) switch
        {
            (ILocalReferenceOperation leftLocal, ILocalReferenceOperation rightLocal) =>
                SymbolEqualityComparer.Default.Equals(leftLocal.Local, rightLocal.Local),
            (IParameterReferenceOperation leftParameter, IParameterReferenceOperation rightParameter) =>
                SymbolEqualityComparer.Default.Equals(leftParameter.Parameter, rightParameter.Parameter),
            (IFieldReferenceOperation leftField, IFieldReferenceOperation rightField) =>
                SymbolEqualityComparer.Default.Equals(leftField.Field, rightField.Field) &&
                AreSameReceiver(leftField.Instance, rightField.Instance),
            (IPropertyReferenceOperation leftProperty, IPropertyReferenceOperation rightProperty) =>
                leftProperty.Arguments.Length is 0 &&
                rightProperty.Arguments.Length is 0 &&
                SymbolEqualityComparer.Default.Equals(leftProperty.Property, rightProperty.Property) &&
                AreSameReceiver(leftProperty.Instance, rightProperty.Instance),
            (IInstanceReferenceOperation leftInstance, IInstanceReferenceOperation rightInstance) =>
                leftInstance.ReferenceKind == rightInstance.ReferenceKind,
            _ => false,
        };
    }

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
