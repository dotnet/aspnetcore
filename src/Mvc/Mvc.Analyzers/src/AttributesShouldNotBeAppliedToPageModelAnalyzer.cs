// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

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

        context.RegisterCompilationStartAction(context =>
        {
            var typeCache = new TypeCache(context.Compilation);
            if (typeCache.PageModelAttribute == null || typeCache.PageModelAttribute.TypeKind == TypeKind.Error)
            {
                // No-op if we can't find types we care about.
                return;
            }

            InitializeWorker(context, typeCache);
        });
    }

    private static void InitializeWorker(CompilationStartAnalysisContext context, TypeCache typeCache)
    {
        context.RegisterSymbolAction(context =>
        {
            var method = (IMethodSymbol)context.Symbol;

            var declaringType = method.ContainingType;
            if (!IsPageModel(declaringType, typeCache.PageModelAttribute) || !IsPageHandlerMethod(method))
            {
                return;
            }

            ReportFilterDiagnostic(context, method, typeCache.IFilterMetadata);
            ReportFilterDiagnostic(context, method, typeCache.AuthorizeAttribute);
            ReportFilterDiagnostic(context, method, typeCache.AllowAnonymousAttribute);

            ReportRouteDiagnostic(context, method, typeCache.IRouteTemplateProvider);
        }, SymbolKind.Method);

        context.RegisterSymbolAction(context =>
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (!IsPageModel(type, typeCache.PageModelAttribute))
            {
                return;
            }

            ReportRouteDiagnosticOnModel(context, type, typeCache.IRouteTemplateProvider);
        }, SymbolKind.NamedType);
    }

    private static bool IsPageHandlerMethod(IMethodSymbol method)
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

    private static void ReportRouteDiagnosticOnModel(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol, INamedTypeSymbol routeAttribute)
    {
        var attribute = GetAttribute(typeSymbol, routeAttribute);
        if (attribute != null)
        {
            var location = GetAttributeLocation(context, attribute);

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MVC1003_RouteAttributesShouldNotBeAppliedToPageModels,
                location,
                attribute.AttributeClass.Name));
        }
    }

    private static void ReportRouteDiagnostic(SymbolAnalysisContext context, IMethodSymbol method, INamedTypeSymbol routeAttribute)
    {
        var attribute = GetAttribute(method, routeAttribute);
        if (attribute != null)
        {
            var location = GetAttributeLocation(context, attribute);

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MVC1002_RouteAttributesShouldNotBeAppliedToPageHandlerMethods,
                location,
                attribute.AttributeClass.Name));
        }
    }

    private static void ReportFilterDiagnostic(SymbolAnalysisContext context, IMethodSymbol method, INamedTypeSymbol filterAttribute)
    {
        var attribute = GetAttribute(method, filterAttribute);
        if (attribute != null)
        {
            var location = GetAttributeLocation(context, attribute);

            context.ReportDiagnostic(Diagnostic.Create(
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

    private static Location GetAttributeLocation(SymbolAnalysisContext context, AttributeData attribute)
    {
        var syntax = attribute.ApplicationSyntaxReference.GetSyntax(context.CancellationToken);
        return syntax?.GetLocation() ?? Location.None;
    }

    private sealed class TypeCache
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
