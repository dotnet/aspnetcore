// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
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
                var typeCache = new SymbolCache(compilationStartAnalysisContext.Compilation);
                if (typeCache.ControllerAttribute == null || typeCache.ControllerAttribute.TypeKind == TypeKind.Error)
                {
                    // No-op if we can't find types we care about.
                    return;
                }

                InitializeWorker(compilationStartAnalysisContext, typeCache);
            });
        }

        private void InitializeWorker(CompilationStartAnalysisContext compilationStartAnalysisContext, SymbolCache symbolCache)
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

            if (SpecifiesModelType(symbolCache, parameter))
            {
                // Ignore parameters that specify a model type.
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

        internal static string GetName(in SymbolCache symbolCache, ISymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes(symbolCache.IModelNameProvider))
            {
                // BindAttribute uses the Prefix property as an alias for IModelNameProvider.Name
                var nameProperty = attribute.AttributeClass == symbolCache.BindAttribute ? "Prefix" : "Name";

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
            public SymbolCache(Compilation compilation)
            {
                BindAttribute = compilation.GetTypeByMetadataName(SymbolNames.BindAttribute);
                ControllerAttribute = compilation.GetTypeByMetadataName(SymbolNames.ControllerAttribute);
                FromBodyAttribute = compilation.GetTypeByMetadataName(SymbolNames.FromBodyAttribute);
                IApiBehaviorMetadata = compilation.GetTypeByMetadataName(SymbolNames.IApiBehaviorMetadata);
                IBinderTypeProviderMetadata = compilation.GetTypeByMetadataName(SymbolNames.IBinderTypeProviderMetadata);
                IModelNameProvider = compilation.GetTypeByMetadataName(SymbolNames.IModelNameProvider);
                NonControllerAttribute = compilation.GetTypeByMetadataName(SymbolNames.NonControllerAttribute);
                NonActionAttribute = compilation.GetTypeByMetadataName(SymbolNames.NonActionAttribute);

                var disposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
                var members = disposable.GetMembers(nameof(IDisposable.Dispose));
                IDisposableDispose = members.Length == 1 ? (IMethodSymbol)members[0] : null;
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
}
