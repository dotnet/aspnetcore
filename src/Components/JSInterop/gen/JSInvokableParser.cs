// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.JSInterop.SourceGenerators;

internal static class JSInvokableParser
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private const string JSInvokableAttributeName = "Microsoft.JSInterop.JSInvokableAttribute";

    public static ParseResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var method = (IMethodSymbol)context.TargetSymbol;
        var location = LocationInfo.From(method.Locations.FirstOrDefault());
        var displayName = method.ContainingType.Name + "." + method.Name;

        if (Validate(method) is { } failure)
        {
            return Failure(failure, location, displayName);
        }

        HostInfo? host = null;
        if (!method.IsStatic && RequiresHost(method.ContainingType))
        {
            if (CreateHost(method.ContainingType, out host) is { } hostFailure)
            {
                return Failure(hostFailure, location, displayName);
            }
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
            var identifier = GetIdentifier(method, attribute);
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
                location,
                host));
        }

        return new(new(methods.ToImmutable()), new(diagnostics.ToImmutable()));
    }

    /// <summary>
    /// Reflection dispatch only sees the attributes declared on the most derived override, so an
    /// override that does not redeclare a base identifier hides it. Records those identifiers so that
    /// generated dispatch does not reach the override through the base type's registration.
    /// </summary>
    public static ParseResult ParseOverride(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not IMethodSymbol { IsStatic: false, IsGenericMethod: false } method
            || method.ContainingType.TypeKind != TypeKind.Class)
        {
            return new(default, default);
        }

        var own = method.GetAttributes()
            .Where(IsJSInvokable)
            .Select(a => GetIdentifier(method, a))
            .ToImmutableHashSet();
        var hidden = ImmutableArray.CreateBuilder<string>();
        for (var overridden = method.OverriddenMethod; overridden is not null; overridden = overridden.OverriddenMethod)
        {
            foreach (var attribute in overridden.GetAttributes().Where(IsJSInvokable))
            {
                if (GetIdentifier(overridden, attribute) is { Length: > 0 } identifier && !own.Contains(identifier) && !hidden.Contains(identifier))
                {
                    hidden.Add(identifier);
                }
            }
        }

        if (hidden.Count == 0)
        {
            return new(default, default);
        }

        var location = LocationInfo.From(method.Locations.FirstOrDefault());
        var displayName = method.ContainingType.Name + "." + method.Name;
        if (ValidateContainingTypes(method.ContainingType) is { } failure)
        {
            return Failure(failure, location, displayName);
        }

        HostInfo? host = null;
        if (RequiresHost(method.ContainingType) && CreateHost(method.ContainingType, out host) is { } hostFailure)
        {
            return Failure(hostFailure, location, displayName);
        }

        var containingType = method.ContainingType.ToDisplayString(TypeFormat);
        return new(
            new([.. hidden.Select(identifier => new JSInvokableMethod(identifier, false, containingType, method.Name, default, ReturnKind.Void, null, location, host, IsHidden: true))]),
            default);
    }

    private static ParseResult Failure((string Id, string[] Args) failure, LocationInfo? location, string displayName)
        => new(default, new([new DiagnosticInfo(failure.Id, location, new([displayName, .. failure.Args]))]));

    private static bool IsJSInvokable(AttributeData attribute)
        => attribute.AttributeClass?.ToDisplayString() == JSInvokableAttributeName;

    private static string? GetIdentifier(IMethodSymbol method, AttributeData attribute)
        => attribute.ConstructorArguments.Length == 1 ? attribute.ConstructorArguments[0].Value as string : method.Name;

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

        // Reflection dispatch skips methods with unbound generic parameters, and only scans exported
        // (therefore non-generic) types for static methods.
        if (method.IsGenericMethod)
        {
            return (DiagnosticDescriptors.Generic.Id, ["generic methods are never invoked"]);
        }

        if (method.IsStatic && IsInGenericType(method.ContainingType))
        {
            return (DiagnosticDescriptors.Generic.Id, ["static methods declared in generic types are never invoked"]);
        }

        if (ValidateContainingTypes(method.ContainingType) is { } containingTypeFailure)
        {
            return containingTypeFailure;
        }

        if (method.IsStatic && !IsPubliclyVisible(method.ContainingType))
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

    private static (string Id, string[] Args)? ValidateContainingTypes(INamedTypeSymbol containingType)
    {
        for (var type = containingType; type is not null; type = type.ContainingType)
        {
            if (type.IsFileLocal)
            {
                return (DiagnosticDescriptors.InaccessibleType.Id, []);
            }
        }

        return null;
    }

    private static bool IsInGenericType(INamedTypeSymbol containingType)
    {
        for (var type = containingType; type is not null; type = type.ContainingType)
        {
            if (type.IsGenericType)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPubliclyVisible(INamedTypeSymbol containingType)
    {
        for (var type = containingType; type is not null; type = type.ContainingType)
        {
            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    // Generated module initializers cannot name type parameters or types that are not accessible to
    // the rest of the assembly.
    private static bool RequiresHost(INamedTypeSymbol containingType)
    {
        for (var type = containingType; type is not null; type = type.ContainingType)
        {
            if (type.IsGenericType || type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
            {
                return true;
            }
        }

        return false;
    }

    private static (string Id, string[] Args)? CreateHost(INamedTypeSymbol containingType, out HostInfo? host)
    {
        host = null;
        var typeName = containingType.ToDisplayString(TypeFormat);
        var declarations = ImmutableArray.CreateBuilder<string>();
        for (var type = containingType; type is not null; type = type.ContainingType)
        {
            var isPartial = type.DeclaringSyntaxReferences.Length > 0 && type.DeclaringSyntaxReferences.All(reference =>
                reference.GetSyntax() is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
            if (!isPartial)
            {
                return (DiagnosticDescriptors.PartialTypeRequired.Id, [typeName]);
            }

            declarations.Insert(0, GetPartialDeclaration(type));
        }

        var ns = containingType.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? containingNamespace.ToDisplayString()
            : null;
        var hintName = new string([.. containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Select(c => char.IsLetterOrDigit(c) || c == '.' ? c : '_')]);
        var baseHosts = ImmutableArray.CreateBuilder<BaseHostInfo>();
        for (var baseType = containingType.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            baseHosts.Add(new(
                baseType.ToDisplayString(TypeFormat),
                baseType.OriginalDefinition.ToDisplayString(TypeFormat),
                baseType.GetMembers("__JSInvokable_Register").Any(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Protected)));
        }

        host = new HostInfo(ns, new(declarations.ToImmutable()), hintName + ".JSInvokable.g.cs", containingType.IsSealed, new(baseHosts.ToImmutable()));
        return null;
    }

    private static string GetPartialDeclaration(INamedTypeSymbol type)
    {
        var keyword = type.TypeKind switch
        {
            TypeKind.Interface => "partial interface",
            // 'partial' must immediately precede the type keyword: 'ref partial struct'.
            TypeKind.Struct => (type.IsRefLikeType ? "ref partial " : "partial ") + (type.IsRecord ? "record struct" : "struct"),
            _ => type.IsRecord ? "partial record" : "partial class",
        };

        var name = SyntaxFactsEscape(type.Name);
        if (type.TypeParameters.Length > 0)
        {
            name += "<" + string.Join(", ", type.TypeParameters.Select(p => p.Variance switch
            {
                VarianceKind.In => "in ",
                VarianceKind.Out => "out ",
                _ => string.Empty,
            } + SyntaxFactsEscape(p.Name))) + ">";
        }

        return keyword + " " + name;
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
