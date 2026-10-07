// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Testing.Generators;

[Generator]
internal class StartupHookGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Static content — always emitted
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource("StartupHook.g.cs", StartupHookSource);
            ctx.AddSource("HostingStartupAttribute.g.cs", HostingStartupAttributeSource);
        });

        // Detect ConfigureServices callsites and emit a resolver
        var callsites = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: IsConfigureServicesCandidate,
                transform: ExtractCallsite)
            .Where(static x => x is not null);

        var collected = callsites.Collect();

        context.RegisterSourceOutput(collected, EmitServiceOverrideResolver);
    }

    static bool IsConfigureServicesCandidate(SyntaxNode node, CancellationToken ct)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        // Fast path: check if the method name looks like ConfigureServices
        return GetInvokedMethodName(invocation) == "ConfigureServices";
    }

    static string GetInvokedMethodName(InvocationExpressionSyntax invocation)
    {
        switch (invocation.Expression)
        {
            case MemberAccessExpressionSyntax memberAccess:
                return memberAccess.Name switch
                {
                    GenericNameSyntax generic => generic.Identifier.Text,
                    IdentifierNameSyntax identifier => identifier.Identifier.Text,
                    _ => ""
                };
            case GenericNameSyntax generic:
                return generic.Identifier.Text;
            case IdentifierNameSyntax identifier:
                return identifier.Identifier.Text;
            default:
                return "";
        }
    }

    static ServiceOverrideCallsite? ExtractCallsite(
        GeneratorSyntaxContext context, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var symbolInfo = semanticModel.GetSymbolInfo(invocation, ct);
        if (symbolInfo.Symbol is not IMethodSymbol method)
        {
            return null;
        }

        // Verify it's ServerStartOptions.ConfigureServices
        if (method.Name != "ConfigureServices" ||
            method.ContainingType?.ToDisplayString() !=
                "Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions")
        {
            return null;
        }

        // Skip calls within ServerStartOptions itself (the generic overload
        // forwards to the non-generic overload with a non-constant parameter)
        var containingSymbol = semanticModel.GetEnclosingSymbol(invocation.SpanStart, ct);
        if (containingSymbol?.ContainingType?.ToDisplayString() ==
            "Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions")
        {
            return null;
        }

        ITypeSymbol? overrideType = null;
        ExpressionSyntax? methodNameExpr = null;

        if (method.IsGenericMethod && method.TypeArguments.Length == 1)
        {
            // Generic: ConfigureServices<T>("methodName")
            overrideType = method.TypeArguments[0];
            if (invocation.ArgumentList.Arguments.Count >= 1)
            {
                methodNameExpr = invocation.ArgumentList.Arguments[0].Expression;
            }
        }
        else if (!method.IsGenericMethod && method.Parameters.Length == 2)
        {
            // Non-generic: ConfigureServices(typeof(T), "methodName")
            if (invocation.ArgumentList.Arguments.Count >= 2)
            {
                var typeArg = invocation.ArgumentList.Arguments[0].Expression;
                if (typeArg is TypeOfExpressionSyntax typeOfExpr)
                {
                    var typeInfo = semanticModel.GetTypeInfo(typeOfExpr.Type, ct);
                    overrideType = typeInfo.Type;
                }

                methodNameExpr = invocation.ArgumentList.Arguments[1].Expression;
            }
        }

        if (overrideType is null || methodNameExpr is null)
        {
            return null;
        }

        // Resolve the method name constant (string literal or nameof)
        var constantValue = semanticModel.GetConstantValue(methodNameExpr, ct);
        if (!constantValue.HasValue || constantValue.Value is not string methodName)
        {
            // Non-constant — the analyzer will report E2E002, skip in generator
            return null;
        }

        // Validate the target method exists with the correct signature
        var targetMethod = overrideType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m =>
                m.IsStatic &&
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() ==
                    "Microsoft.Extensions.DependencyInjection.IServiceCollection");

        if (targetMethod is null)
        {
            // Invalid callsite — the analyzer will report E2E001/E2E003, skip in generator
            return null;
        }

        var fullyQualified = overrideType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat);
        var fullName = overrideType.ToDisplayString();
        var assemblyName = overrideType.ContainingAssembly?.Name ?? "";

        return new ServiceOverrideCallsite(fullyQualified, fullName, assemblyName, methodName);
    }

    static void EmitServiceOverrideResolver(
        SourceProductionContext context,
        ImmutableArray<ServiceOverrideCallsite?> callsites)
    {
        var distinct = callsites
            .Where(c => c is not null)
            .Cast<ServiceOverrideCallsite>()
            .Distinct()
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.AspNetCore.Components.Testing.Generated;");
        sb.AppendLine();
        sb.AppendLine("internal sealed class ServiceOverrideResolver : global::Microsoft.AspNetCore.Components.Testing.Infrastructure.IE2EServiceOverrideResolver");
        sb.AppendLine("{");
        sb.AppendLine("    public global::System.Action<global::Microsoft.Extensions.DependencyInjection.IServiceCollection>? TryResolve(");
        sb.AppendLine("        string assemblyQualifiedTypeName, string methodName)");
        sb.AppendLine("    {");

        if (distinct.Count > 0)
        {
            // Group callsites by the override type
            var groups = distinct
                .GroupBy(c => new { c.TypeFullyQualifiedName, c.TypeFullName, c.AssemblyName })
                .ToList();

            foreach (var group in groups)
            {
                var typePrefix = group.Key.TypeFullName + ", " + group.Key.AssemblyName + ",";
                sb.AppendLine($"        if (assemblyQualifiedTypeName.StartsWith(\"{typePrefix}\", global::System.StringComparison.Ordinal))");
                sb.AppendLine("        {");
                sb.AppendLine("            return methodName switch");
                sb.AppendLine("            {");

                foreach (var callsite in group)
                {
                    sb.AppendLine($"                \"{callsite.MethodName}\" => {callsite.TypeFullyQualifiedName}.{callsite.MethodName},");
                }

                sb.AppendLine("                _ => null,");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }

        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("ServiceOverrideResolver.g.cs", sb.ToString());
    }

    private const string StartupHookSource = """
        // <auto-generated/>
        using System;
        using System.IO;
        using System.Reflection;
        using System.Runtime.Loader;

        internal class StartupHook
        {
            public static void Initialize()
            {
                var startupHookAssembly = Assembly.GetExecutingAssembly();
                var testBinDirectory = Path.GetDirectoryName(startupHookAssembly.Location);

                AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
                {
                    var candidatePath = Path.Combine(testBinDirectory, assemblyName.Name + ".dll");
                    if (File.Exists(candidatePath))
                    {
                        return context.LoadFromAssemblyPath(candidatePath);
                    }
                    return null;
                };
            }
        }
        """;

    private const string HostingStartupAttributeSource = """
        // <auto-generated/>
        [assembly: Microsoft.AspNetCore.Hosting.HostingStartup(typeof(Microsoft.AspNetCore.Components.Testing.Infrastructure.TestReadinessHostingStartup))]
        """;
}
