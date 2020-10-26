// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AttributesShouldNotBeAppliedToPageModelAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods,
            DiagnosticDescriptors.MVC1002_RouteAttributesShouldNotBeAppliedToPageHandlerMethods,
            DiagnosticDescriptors.MVC1003_RouteAttributesShouldNotBeAppliedToPageModels);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
            {
                var typeCache = new TypeCache(compilationStartAnalysisContext.Compilation);
                if (typeCache.PageModelAttribute == null || typeCache.PageModelAttribute.TypeKind == TypeKind.Error)
                {
                    // No-op if we can't find types we care about.
                    return;
                }

                InitializeWorker(compilationStartAnalysisContext, typeCache);
            });
        }

        private void InitializeWorker(CompilationStartAnalysisContext compilationStartAnalysisContext, TypeCache typeCache)
        {
            compilationStartAnalysisContext.RegisterSymbolAction(symbolAnalysisContext =>
            {
                var method = (IMethodSymbol)symbolAnalysisContext.Symbol;

                var declaringType = method.ContainingType;
                if (!IsPageModel(declaringType, typeCache.PageModelAttribute) || !IsPageHandlerMethod(method))
                {
                    return;
                }

                ReportFilterDiagnostic(ref symbolAnalysisContext, method, typeCache.IFilterMetadata);
                ReportFilterDiagnostic(ref symbolAnalysisContext, method, typeCache.AuthorizeAttribute);
                ReportFilterDiagnostic(ref symbolAnalysisContext, method, typeCache.AllowAnonymousAttribute);

                ReportRouteDiagnostic(ref symbolAnalysisContext, method, typeCache.IRouteTemplateProvider);
            }, SymbolKind.Method);

            compilationStartAnalysisContext.RegisterSymbolAction(symbolAnalysisContext =>
            {
                var type = (INamedTypeSymbol)symbolAnalysisContext.Symbol;
                if (!IsPageModel(type, typeCache.PageModelAttribute))
                {
                    return;
                }

                ReportRouteDiagnosticOnModel(ref symbolAnalysisContext, type, typeCache.IRouteTemplateProvider);
            }, SymbolKind.NamedType);
        }

        private bool IsPageHandlerMethod(IMethodSymbol method)
        {
            return method.MethodKind == MethodKind.Ordinary &&
                !method.IsStatic &&
                !method.IsGenericMethod &&
                method.DeclaredAccessibility == Accessibility.Public;
        }

        private static bool IsPageModel(INamedTypeSymbol type, INamedTypeSymbol pageAttributeModel)
        {
            return type.TypeKind == TypeKind.Class &&
                !type.IsStatic &&
                type.HasAttribute(pageAttributeModel, inherit: true);
        }

        private static void ReportRouteDiagnosticOnModel(ref SymbolAnalysisContext symbolAnalysisContext, INamedTypeSymbol typeSymbol, INamedTypeSymbol routeAttribute)
        {
            var attribute = GetAttribute(typeSymbol, routeAttribute);
            if (attribute != null)
            {
                var location = GetAttributeLocation(ref symbolAnalysisContext, attribute);

                symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MVC1003_RouteAttributesShouldNotBeAppliedToPageModels,
                    location,
                    attribute.AttributeClass.Name));
            }
        }

        private static void ReportRouteDiagnostic(ref SymbolAnalysisContext symbolAnalysisContext, IMethodSymbol method, INamedTypeSymbol routeAttribute)
        {
            var attribute = GetAttribute(method, routeAttribute);
            if (attribute != null)
            {
                var location = GetAttributeLocation(ref symbolAnalysisContext, attribute);

                symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MVC1002_RouteAttributesShouldNotBeAppliedToPageHandlerMethods,
                    location,
                    attribute.AttributeClass.Name));
            }
        }

        private static void ReportFilterDiagnostic(ref SymbolAnalysisContext symbolAnalysisContext, IMethodSymbol method, INamedTypeSymbol filterAttribute)
        {
            var attribute = GetAttribute(method, filterAttribute);
            if (attribute != null)
            {
                var location = GetAttributeLocation(ref symbolAnalysisContext, attribute);

                symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MVC1001_FiltersShouldNotBeAppliedToPageHandlerMethods,
                    location,
                    attribute.AttributeClass.Name));
            }
        }

        private static AttributeData? GetAttribute(ISymbol symbol, INamedTypeSymbol attributeType)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attributeType.IsAssignableFrom(attribute.AttributeClass))
                {
                    return attribute;
                }
            }

            return null;
        }

        private static Location GetAttributeLocation(ref SymbolAnalysisContext symbolAnalysisContext, AttributeData attribute)
        {
            var syntax = attribute.ApplicationSyntaxReference.GetSyntax(symbolAnalysisContext.CancellationToken);
            return syntax?.GetLocation() ?? Location.None;
        }

        private class TypeCache
        {
            public TypeCache(Compilation compilation)
            {
                PageModelAttribute = compilation.GetTypeByMetadataName(SymbolNames.PageModelAttributeType);
                IFilterMetadata = compilation.GetTypeByMetadataName(SymbolNames.IFilterMetadataType);
                AuthorizeAttribute = compilation.GetTypeByMetadataName(SymbolNames.AuthorizeAttribute);
                AllowAnonymousAttribute = compilation.GetTypeByMetadataName(SymbolNames.AllowAnonymousAttribute);
                IRouteTemplateProvider = compilation.GetTypeByMetadataName(SymbolNames.IRouteTemplateProvider);
            }

            public INamedTypeSymbol PageModelAttribute { get; }

            public INamedTypeSymbol IFilterMetadata { get; }

            public INamedTypeSymbol AuthorizeAttribute { get; }

            public INamedTypeSymbol AllowAnonymousAttribute { get; }

            public INamedTypeSymbol IRouteTemplateProvider { get; }
        }
    }
}
