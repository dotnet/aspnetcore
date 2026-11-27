// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.JSInterop.SourceGenerators;

internal static class JSInvokableParser
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static ParseResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var method = (IMethodSymbol)context.TargetSymbol;
        var location = LocationInfo.From(method.Locations.FirstOrDefault());
        var displayName = method.ContainingType.Name + "." + method.Name;

        if (Validate(method) is { } failure)
        {
            return new(default, new([new DiagnosticInfo(failure.Id, location, new([displayName, .. failure.Args]))]));
        }

        var compilation = context.SemanticModel.Compilation;
        var (returnKind, resultType) = ClassifyReturn(method.ReturnType, compilation);
        var parameterTypes = new EquatableArray<string>([.. method.Parameters.Select(p => p.Type.ToDisplayString(TypeFormat))]);
        var containingType = method.ContainingType.ToDisplayString(TypeFormat);
        var methodName = SyntaxFactsEscape(method.Name);

        var methods = ImmutableArray.CreateBuilder<JSInvokableMethod>();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        foreach (var attribute in context.Attributes)
        {
            // [JSInvokable] allows multiple identifiers; each one is registered independently.
            string? identifier = method.Name;
            if (attribute.ConstructorArguments.Length == 1)
            {
                identifier = attribute.ConstructorArguments[0].Value as string;
            }

            if (string.IsNullOrEmpty(identifier))
            {
                diagnostics.Add(new(DiagnosticDescriptors.UnsupportedSignature.Id, location, new([displayName, "the identifier must not be null or empty"])));
                continue;
            }

            methods.Add(new JSInvokableMethod(
                identifier!,
                method.IsStatic,
                containingType,
                methodName,
                parameterTypes,
                returnKind,
                resultType,
                location));
        }

        return new(new(methods.ToImmutable()), new(diagnostics.ToImmutable()));
    }

    private static (string Id, string[] Args)? Validate(IMethodSymbol method)
    {
        if (method.MethodKind != MethodKind.Ordinary)
        {
            return (DiagnosticDescriptors.UnsupportedSignature.Id, ["only ordinary methods are supported"]);
        }

        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            return (DiagnosticDescriptors.NotPublic.Id, []);
        }

        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.IsGenericType)
            {
                return (DiagnosticDescriptors.Generic.Id, []);
            }
        }

        if (method.IsGenericMethod)
        {
            return (DiagnosticDescriptors.Generic.Id, []);
        }

        var publiclyVisible = true;
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.IsFileLocal)
            {
                return (DiagnosticDescriptors.InaccessibleType.Id, []);
            }

            switch (type.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    break;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    publiclyVisible = false;
                    break;
                default:
                    return (DiagnosticDescriptors.InaccessibleType.Id, []);
            }
        }

        if (method.IsStatic && !publiclyVisible)
        {
            // Reflection dispatch only discovers static methods on exported types.
            return (DiagnosticDescriptors.StaticOnNonPublicType.Id, []);
        }

        if (method.IsAbstract || method.IsVirtual && method.IsStatic || (!method.IsStatic && method.ContainingType.TypeKind == TypeKind.Interface))
        {
            return (DiagnosticDescriptors.UnsupportedSignature.Id, ["abstract, static virtual and interface instance methods are not supported"]);
        }

        if (!method.IsStatic && method.ContainingType.IsValueType)
        {
            return (DiagnosticDescriptors.UnsupportedSignature.Id, ["instance methods on value types are not supported"]);
        }

        if (method.ReturnsByRef || method.ReturnsByRefReadonly || IsUnsupportedType(method.ReturnType))
        {
            return (DiagnosticDescriptors.UnsupportedSignature.Id, ["by-reference, ref-like and pointer return types are not supported"]);
        }

        foreach (var parameter in method.Parameters)
        {
            if (parameter.RefKind != RefKind.None || IsUnsupportedType(parameter.Type))
            {
                return (DiagnosticDescriptors.UnsupportedSignature.Id, [$"parameter '{parameter.Name}' is by-reference, ref-like or a pointer"]);
            }
        }

        return null;
    }

    private static bool IsUnsupportedType(ITypeSymbol type)
        => type.IsRefLikeType || type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer;

    private static (ReturnKind Kind, string? ResultType) ClassifyReturn(ITypeSymbol returnType, Compilation compilation)
    {
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return (ReturnKind.Void, null);
        }

        var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (returnType is INamedTypeSymbol named)
        {
            if (SymbolEqualityComparer.Default.Equals(named, valueTask))
            {
                return (ReturnKind.Task, null);
            }

            if (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, valueTaskOfT))
            {
                return (ReturnKind.TaskOfT, named.TypeArguments[0].ToDisplayString(TypeFormat));
            }

            // Like reflection dispatch, a type deriving from Task is awaited as a task.
            for (INamedTypeSymbol? type = named; type is not null; type = type.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, taskOfT))
                {
                    return (ReturnKind.TaskOfT, type.TypeArguments[0].ToDisplayString(TypeFormat));
                }

                if (SymbolEqualityComparer.Default.Equals(type, task))
                {
                    return (ReturnKind.Task, null);
                }
            }
        }

        return (ReturnKind.Value, returnType.ToDisplayString(TypeFormat));
    }

    private static string SyntaxFactsEscape(string name)
        => Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(name) != Microsoft.CodeAnalysis.CSharp.SyntaxKind.None ? "@" + name : name;
}
