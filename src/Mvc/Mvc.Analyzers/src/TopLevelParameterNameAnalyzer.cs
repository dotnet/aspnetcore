// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TopLevelParameterNameAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.MVC1004_ParameterNameCollidesWithTopLevelProperty);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
        {
            if (!SymbolCache.TryCreate(compilationStartAnalysisContext.Compilation, out var typeCache))
            {
                // No-op if we can't find types we care about.
                return;
            }

            InitializeWorker(compilationStartAnalysisContext, typeCache);
        });
    }

    private static void InitializeWorker(CompilationStartAnalysisContext compilationStartAnalysisContext, SymbolCache symbolCache)
    {
        compilationStartAnalysisContext.RegisterSymbolAction(symbolAnalysisContext =>
        {
            var method = (IMethodSymbol)symbolAnalysisContext.Symbol;
            if (method.MethodKind != MethodKind.Ordinary)
            {
                return;
            }

            if (method.Parameters.Length == 0)
            {
                return;
            }

            if (!MvcFacts.IsController(method.ContainingType, symbolCache.ControllerAttribute, symbolCache.NonControllerAttribute) ||
                !MvcFacts.IsControllerAction(method, symbolCache.NonActionAttribute, symbolCache.IDisposableDispose))
            {
                return;
            }

            if (method.ContainingType.HasAttribute(symbolCache.IApiBehaviorMetadata, inherit: true))
            {
                // The issue of parameter name collision with properties affects complex model-bound types
                // and not input formatting. Ignore ApiController instances since they default to formatting.
                return;
            }

            for (var i = 0; i < method.Parameters.Length; i++)
            {
                var parameter = method.Parameters[i];
                if (IsProblematicParameter(symbolCache, parameter))
                {
                    var location = parameter.Locations.Length != 0 ?
                        parameter.Locations[0] :
                        Location.None;

                    symbolAnalysisContext.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.MVC1004_ParameterNameCollidesWithTopLevelProperty,
                            location,
                            parameter.Type.Name,
                            parameter.Name));
                }
            }
        }, SymbolKind.Method);
    }

    internal static bool IsProblematicParameter(in SymbolCache symbolCache, IParameterSymbol parameter)
    {
        if (parameter.GetAttributes(symbolCache.FromBodyAttribute).Any())
        {
            // Ignore input formatted parameters.
            return false;
        }

        if (SpecifiesModelType(in symbolCache, parameter))
        {
            // Ignore parameters that specify a model type.
            return false;
        }

        if (!IsComplexType(parameter.Type))
        {
            return false;
        }

        var parameterName = GetName(symbolCache, parameter);

        var type = parameter.Type;
        while (type != null)
        {
            foreach (var member in type.GetMembers())
            {
                if (member.DeclaredAccessibility != Accessibility.Public ||
                    member.IsStatic ||
                    member.Kind != SymbolKind.Property)
                {
                    continue;
                }

                var propertyName = GetName(symbolCache, member);
                if (string.Equals(parameterName, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            type = type.BaseType;
        }

        return false;
    }

    private static bool IsComplexType(ITypeSymbol type)
    {
        // This analyzer should not apply to simple types. In MVC, a simple type is any type that has a type converter that returns true for TypeConverter.CanConvertFrom(typeof(string)).
        // Unfortunately there isn't a Roslyn way of determining if a TypeConverter exists for a given symbol or if the converter allows string conversions.
        // https://github.com/dotnet/corefx/blob/v3.0.0-preview8.19405.3/src/System.ComponentModel.TypeConverter/src/System/ComponentModel/ReflectTypeDescriptionProvider.cs#L103-L141
        // provides a list of types that have built-in converters.
        // We'll use a simpler heuristic in the analyzer: A type is simple if it's a value type or if it's in the "System.*" namespace hierarchy.

        var @namespace = type.ContainingNamespace?.ToString();
        if (@namespace != null)
        {
            // Things in the System.* namespace hierarchy don't count as complex types. This workarounds
            // the problem of discovering type converters on types in mscorlib.
            return @namespace != "System" &&
                !@namespace.StartsWith("System.", StringComparison.Ordinal);
        }

        return true;
    }

    internal static string GetName(in SymbolCache symbolCache, ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes(symbolCache.IModelNameProvider))
        {
            // BindAttribute uses the Prefix property as an alias for IModelNameProvider.Name
            var nameProperty = SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbolCache.BindAttribute) ? "Prefix" : "Name";

            // All of the built-in attributes (FromQueryAttribute, ModelBinderAttribute etc) only support setting the name via
            // a property. We'll ignore constructor values.
            for (var i = 0; i < attribute.NamedArguments.Length; i++)
            {
                var namedArgument = attribute.NamedArguments[i];
                var namedArgumentValue = namedArgument.Value;
                if (string.Equals(namedArgument.Key, nameProperty, StringComparison.Ordinal) &&
                    namedArgumentValue.Kind == TypedConstantKind.Primitive &&
                    namedArgumentValue.Type.SpecialType == SpecialType.System_String &&
                    namedArgumentValue.Value is string name)
                {
                    return name;
                }
            }
        }

        return symbol.Name;
    }

    internal static bool SpecifiesModelType(in SymbolCache symbolCache, IParameterSymbol parameterSymbol)
    {
        foreach (var attribute in parameterSymbol.GetAttributes(symbolCache.IBinderTypeProviderMetadata))
        {
            // Look for a attribute property named BinderType being assigned. This would match
            // [ModelBinder(BinderType = typeof(SomeBinder))]
            for (var i = 0; i < attribute.NamedArguments.Length; i++)
            {
                var namedArgument = attribute.NamedArguments[i];
                var namedArgumentValue = namedArgument.Value;
                if (string.Equals(namedArgument.Key, "BinderType", StringComparison.Ordinal) &&
                    namedArgumentValue.Kind == TypedConstantKind.Type)
                {
                    return true;
                }
            }

            // Look for the binder type being specified in the constructor. This would match
            // [ModelBinder(typeof(SomeBinder))]
            var constructorParameters = attribute.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
            for (var i = 0; i < constructorParameters.Length; i++)
            {
                if (string.Equals(constructorParameters[i].Name, "binderType", StringComparison.Ordinal))
                {
                    // A constructor that requires binderType was used.
                    return true;
                }
            }
        }

        return false;
    }

    internal readonly struct SymbolCache
    {
        public SymbolCache(
            INamedTypeSymbol bindAttribute,
            INamedTypeSymbol controllerAttribute,
            INamedTypeSymbol fromBodyAttribute,
            INamedTypeSymbol apiBehaviorMetadata,
            INamedTypeSymbol binderTypeProviderMetadata,
            INamedTypeSymbol modelNameProvider,
            INamedTypeSymbol nonControllerAttribute,
            INamedTypeSymbol nonActionAttribute,
            IMethodSymbol disposableDispose)
        {
            BindAttribute = bindAttribute;
            ControllerAttribute = controllerAttribute;
            FromBodyAttribute = fromBodyAttribute;
            IApiBehaviorMetadata = apiBehaviorMetadata;
            IBinderTypeProviderMetadata = binderTypeProviderMetadata;
            IModelNameProvider = modelNameProvider;
            NonControllerAttribute = nonControllerAttribute;
            NonActionAttribute = nonActionAttribute;
            IDisposableDispose = disposableDispose;
        }

        public static bool TryCreate(Compilation compilation, out SymbolCache symbolCache)
        {
            symbolCache = default;

            if (!TryGetType(SymbolNames.BindAttribute, out var bindAttribute))
            {
                return false;
            }

            if (!TryGetType(SymbolNames.ControllerAttribute, out var controllerAttribute))
            {
                return false;
            }

            if (!TryGetType(SymbolNames.FromBodyAttribute, out var fromBodyAttribute))
            {
                return false;
            }

            if (!TryGetType(SymbolNames.IApiBehaviorMetadata, out var apiBehaviorMetadata))
            {
                return false;
            }

            if (!TryGetType(SymbolNames.IBinderTypeProviderMetadata, out var iBinderTypeProviderMetadata))
            {
                return false;
            }

            if (!TryGetType(SymbolNames.IModelNameProvider, out var iModelNameProvider))
            {
                return false;
            }

            if (!TryGetType(SymbolNames.NonControllerAttribute, out var nonControllerAttribute))
            {
                return false;
            }

            if (!TryGetType(SymbolNames.NonActionAttribute, out var nonActionAttribute))
            {
                return false;
            }

            var disposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            var members = disposable?.GetMembers(nameof(IDisposable.Dispose));
            var idisposableDispose = (IMethodSymbol?)members?[0];
            if (idisposableDispose == null)
            {
                return false;
            }

            symbolCache = new SymbolCache(
                bindAttribute,
                controllerAttribute,
                fromBodyAttribute,
                apiBehaviorMetadata,
                iBinderTypeProviderMetadata,
                iModelNameProvider,
                nonControllerAttribute,
                nonActionAttribute,
                idisposableDispose);

            return true;

            bool TryGetType(string typeName, out INamedTypeSymbol typeSymbol)
            {
                typeSymbol = compilation.GetTypeByMetadataName(typeName);
                return typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error;
            }
        }

        public INamedTypeSymbol BindAttribute { get; }
        public INamedTypeSymbol ControllerAttribute { get; }
        public INamedTypeSymbol FromBodyAttribute { get; }
        public INamedTypeSymbol IApiBehaviorMetadata { get; }
        public INamedTypeSymbol IBinderTypeProviderMetadata { get; }
        public INamedTypeSymbol IModelNameProvider { get; }
        public INamedTypeSymbol NonControllerAttribute { get; }
        public INamedTypeSymbol NonActionAttribute { get; }
        public IMethodSymbol IDisposableDispose { get; }
    }
}
