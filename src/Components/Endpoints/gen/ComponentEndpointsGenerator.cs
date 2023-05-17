// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Endpoints.Generator;

[Generator]
public sealed class ComponentEndpointsGenerator : IIncrementalGenerator
{
    private static readonly ImmutableArray<string> _yieldBreakStatement = ImmutableArray.Create("yield break;");
    // We are going to generate a file like this one (with some simplifications in the sample),
    // assuming an app Blazor.United.Assembly and a library Razor.Class.Library:
    //[assembly: AppRazorComponentApplication]
    //namespace Microsoft.AspNetCore.Components.Infrastructure;

    //file class AppRazorComponentApplicationAttribute : RazorComponentApplicationAttribute
    //{
    //    public override ComponentApplicationBuilder GetBuilder()
    //    {
    //        var builder = new ComponentApplicationBuilder();
    //        builder.AddLibrary(GetBlazorUnitedAssemblyBuilder());
    //        builder.AddLibrary(GetRazorClassLibraryBuilder());
    //        return builder;
    //    }
    //
    //    private ComponentLibraryBuilder GetBlazorUnitedAssemblyBuilder()
    //    {
    //        var source = "Blazor.United.Assembly";
    //        return new ComponentLibraryBuilder(
    //              source,
    //              GetBlazorUnitedAssemblyPages(source),
    //              GetBlazorUnitedAssemblyComponents(source));
    //    }
    //
    //    private IEnumerable<PageComponentBuilder> GetBlazorUnitedAssemblyPages()
    //    {
    //        yield return new PageComponentBuilder()
    //        {
    //            Source = "Blazor.United.Assembly",
    //            PageType = typeof(Counter),
    //            RouteTemplates = new List<string> { "/counter" }
    //        };
    //        ...
    //    }
    //    ...
    //}
    // This approach has been chosen for a couple of reasons:
    // 1) We want to avoid creating very big methods at compile time (We might even need to split Get...(Pages|Components) into chunks
    //    to limit the method size.
    // 2) We want the source generator to be as incremental as possible, so we are going to compute the thunk bodies individually and reuse
    //    them when possible. The structure above, mostly relates to the following thunks:
    // pagesTunk:
    //     yield return new PageComponentBuilder(string source)
    //     {
    //         Source = source,
    //         PageType = typeof(Counter),
    //         RouteTemplates = new List<string> { "/counter" }
    //     };
    //     ...
    // libraryThunk:
    //    private ComponentLibraryBuilder GetBlazorUnitedAssemblyBuilder()
    //    {
    //        var source = "Blazor.United.Assembly";
    //        return new ComponentLibraryBuilder(
    //              source,
    //              GetBlazorUnitedAssemblyPages(source),
    //              GetBlazorUnitedAssemblyComponents(source));
    //    }
    //
    //    private IEnumerable<PageComponentBuilder> GetBlazorUnitedAssemblyPages()
    //    {
    //        <<pagesThunk>>
    //    }
    // appThunk:
    //    var builder = new ComponentApplicationBuilder();
    //    builder.AddLibrary(GetBlazorUnitedAssemblyBuilder());
    //    builder.AddLibrary(GetRazorClassLibraryInfo());
    //    return builder;
    //
    //
    // appThunk only changes with renames.
    // libraryThunk only changes when the project is renamed
    // pagesThunk changes when a page is added, removed or renamed, and so on.
    // This drives the way we approach writing the code for the source generator, since we favor lots of small methods and
    // combinations over big and coarse methods that compute the entire contents.
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var wellKnownTypes = context.CompilationProvider.Select(static (compilation, ct) => WellKnownTypes.GetOrCreate(compilation));

        var componentInterface = wellKnownTypes.Select(
            static (wkt, ct) => wkt.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Components_IComponent));

        var routeAttribute = wellKnownTypes.Select(
            static (wkt, ct) => wkt.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Components_RouteAttribute));

        var renderModeAttribute = wellKnownTypes.Select(
            static (wkt, ct) => wkt.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Components_RenderModeAttribute));

        var componentsAssemblySymbol = componentInterface.Select(static (ci, ct) => ci.ContainingAssembly);

        var componentsFromProject = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (sn, ct) => sn is ClassDeclarationSyntax cls &&
                sn.IsKind(SyntaxKind.ClassDeclaration) &&
                cls.BaseList != null && cls.BaseList.Types.Count > 0,
                transform: static (ctx, ct) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, cancellationToken: ct))
            .Combine(componentInterface)
            .Where(IsComponent)
            .Combine(routeAttribute)
            .Combine(renderModeAttribute)
            .Select(CreateComponentModel)
            .Combine(context.CompilationProvider.Select(static (c, ct) => c.Assembly))
            .Select(static (pair, ct) => (pair.Right, pair.Left));

        var compilationReferences = context.CompilationProvider
            .SelectMany(static (c, t) => c.References.Select(r => (IAssemblySymbol)c.GetAssemblyOrModuleSymbol(r)!));

        var assembliesReferencingComponents = compilationReferences
            .Combine(componentsAssemblySymbol)
            .Where(FilterAssemblies)
            .Select(static (c, _) => c.Left);

        var getLibraryComponentMethodThunks = assembliesReferencingComponents
            .Select(static (arc, ct) => Emitter.CreateGetLibraryMethodThunk(arc));

        var appGetLibraryComponentMethodThunk = context.CompilationProvider
            .Select(static (c, ct) => Emitter.CreateGetLibraryMethodThunk(c.Assembly));

        var referencesGetLibraryThunk = assembliesReferencingComponents
            .Select(static (arc, ct) => Emitter.CreateLibraryThunk(arc));

        var appGetLibraryThunk = context.CompilationProvider
            .Select(static (c, ct) => Emitter.CreateLibraryThunk(c.Assembly));

        var getBuilderThunk = referencesGetLibraryThunk
            .Collect()
            .Combine(appGetLibraryThunk)
            .Select(static (t, ct) => t.Left.Add(t.Right))
            .Select(static (getLibraryThunks, ct) => Emitter.CreateGetBuilderThunk(getLibraryThunks));

        var referencedAssembliesComponents = assembliesReferencingComponents
            .Combine(componentInterface)
            .Combine(routeAttribute)
            .Combine(renderModeAttribute)
            .SelectMany(ComponentWithAssembly);

        var (projectGetComponentPagesBodyThunk, getPagesMethodThunks) = CreateGetPagesMethodThunks(
            context,
            componentsFromProject,
            assembliesReferencingComponents,
            referencedAssembliesComponents);

        var (projectGetComponentsBodyThunk, getComponentsMethodThunks) = CreateGetComponentsMethodThunks(
                context,
                componentsFromProject,
                assembliesReferencingComponents,
                referencedAssembliesComponents);

        var allThunks = getLibraryComponentMethodThunks
            .Collect()
            .Combine(appGetLibraryComponentMethodThunk)
            .Combine(getBuilderThunk)
            .Combine(getComponentsMethodThunks.Collect())
            .Combine(projectGetComponentPagesBodyThunk)
            .Combine(projectGetComponentsBodyThunk)
            .Combine(getPagesMethodThunks.Collect());

        context.RegisterImplementationSourceOutput(allThunks, static (spc, thunks) =>
        {
            // These APIs are heavily biased towards creating lists by nesting tuples.
            // These lists are formed by tuples where the head is on the right element and the tail on the left.
            // We avoid constructing intermediate arrays because every time we do so is an additional
            // array copy we need to make
            var ((((((
                getLibraryComponentMethodThunks,
                appGetLibraryComponentMethodThunk),
                getBuilderThunk),
                getComponentMethodThunks),
                projectGetComponentPagesBodyThunk),
                projectGetComponentComponentsBodyThunk),
                getPagesMethodThunks) = thunks;

            var stringBuilder = new StringBuilder();
            using var stringWriter = new StringWriter(stringBuilder);
            var codeWriter = new CodeWriter(stringWriter, 0);
            codeWriter.WriteLine(ComponentEndpointsGeneratorSources.SourceHeader);
            codeWriter.WriteLine();
            codeWriter.WriteLine(ComponentEndpointsGeneratorSources.RazorComponentApplicationAssemblyAndNamespaceDeclaration);
            codeWriter.WriteLine();
            codeWriter.WriteLine(ComponentEndpointsGeneratorSources.GeneratedCodeAttribute);
            codeWriter.WriteLine(ComponentEndpointsGeneratorSources.RazorComponentApplicationAttributeFileHeader);
            codeWriter.StartBlock();
            WriteThunks(codeWriter, getComponentMethodThunks);
            codeWriter.WriteLine(projectGetComponentComponentsBodyThunk);
            WriteThunks(codeWriter, getPagesMethodThunks);
            codeWriter.WriteLine(projectGetComponentPagesBodyThunk);
            WriteThunks(codeWriter, getLibraryComponentMethodThunks);
            codeWriter.WriteLine(appGetLibraryComponentMethodThunk);
            codeWriter.Write(getBuilderThunk);
            codeWriter.EndBlock(newLine: false);

            codeWriter.Flush();
            stringWriter.Flush();

            var fileText = stringBuilder.ToString();
            spc.AddSource("Components.Discovery.g.cs", fileText);

            static void WriteThunks(CodeWriter codeWriter, ImmutableArray<string> thunks)
            {
                for (var i = 0; i < thunks.Length; i++)
                {
                    var thunk = thunks[i];
                    codeWriter.WriteLine(thunk);
                }
            }
        });
    }

    private static (IncrementalValueProvider<string> projectGetPagesThunk, IncrementalValuesProvider<string> getPagesThunks)
        CreateGetPagesMethodThunks(
            IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<(IAssemblySymbol Right, ComponentModel Left)> componentsFromProject,
            IncrementalValuesProvider<IAssemblySymbol> assembliesReferencingComponents,
            IncrementalValuesProvider<ComponentModel> allComponentDefinitions)
    {
        var getPagesSignature = assembliesReferencingComponents.Select(
            static (t, ct) => (assembly: t, signature: Emitter.CreateGetPagesMethodSignature(t)));

        var projectGetPagesSignature = context.CompilationProvider.Select(static (c, ct) => c.Assembly)
            .Select(static (assembly, ct) => (assembly, signature: Emitter.CreateGetPagesMethodSignature(assembly)));

        var projectGetComponentPagesBodyThunk = componentsFromProject
            .Where(cfp => cfp.Left.IsPage)
            .Select(static (cm, ct) => Emitter.GetPagesBody(cm.Left))
            .Collect()
            .Combine(projectGetPagesSignature)
            .Select(static (ctx, ct) => Emitter.CreateGetMethod(ctx.Right.signature, ctx.Left));
        var getComponentPagesBodyThunk = allComponentDefinitions
            .Where(c => c.IsPage)
            .Select(static (cm, ct) => (cm.Component.ContainingAssembly, body: Emitter.GetPagesBody(cm)));

        var groupedComponentPageStatements = getComponentPagesBodyThunk
            .Collect()
            .Select(static (gpbt, ct) => gpbt.GroupBy(kvp => kvp.ContainingAssembly, SymbolEqualityComparer.Default)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Select(t => t.body).ToImmutableArray(), SymbolEqualityComparer.Default));

        var getPagesMethodThunks = getPagesSignature
            .Combine(groupedComponentPageStatements)
            .Select(static (ctx, ct) =>
            {
                var (assembly, signature) = ctx.Left;
                var bodyStatements = ctx.Right;
                var body = bodyStatements.TryGetValue(assembly, out var found) ? found : _yieldBreakStatement;
                return Emitter.CreateGetMethod(signature, body);
            });

        return (projectGetComponentPagesBodyThunk, getPagesMethodThunks);
    }

    private static (IncrementalValueProvider<string> projectGetComponentsThunk, IncrementalValuesProvider<string> getComponentsThunks)
        CreateGetComponentsMethodThunks(
            IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<(IAssemblySymbol Right, ComponentModel Left)> componentsFromProject,
            IncrementalValuesProvider<IAssemblySymbol> assembliesReferencingComponents,
            IncrementalValuesProvider<ComponentModel> referencedAssembliesComponents)
    {
        var projectGetComponentsSignature = context.CompilationProvider.Select(static (c, ct) => c.Assembly)
            .Select(static (assembly, ct) => (assembly, signature: Emitter.CreateGetComponentsMethodSignature(assembly)));

        var projectGetComponentComponentsBodyThunk = componentsFromProject
            .Select(static (cm, ct) => Emitter.GetComponentsBody(cm.Left))
            .Collect()
            .Combine(projectGetComponentsSignature)
            .Select(static (ctx, ct) => Emitter.CreateGetMethod(ctx.Right.signature, ctx.Left));

        var getComponentsSignature = assembliesReferencingComponents.Select(
            static (assembly, ct) => (assembly, signature: Emitter.CreateGetComponentsMethodSignature(assembly)));

        var getComponentsBodyThunk = referencedAssembliesComponents
            .Select(static (cm, ct) => (cm.Component.ContainingAssembly, body: Emitter.GetComponentsBody(cm)));

        var groupedComponentStatements = getComponentsBodyThunk
            .Collect()
            .Select(static (gpbt, ct) => gpbt.GroupBy(kvp => kvp.ContainingAssembly, SymbolEqualityComparer.Default)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Select(t => t.body).ToImmutableArray(), SymbolEqualityComparer.Default));

        var getComponentMethodThunks = getComponentsSignature
            .Combine(groupedComponentStatements)
            .Select(static (ctx, ct) =>
            {
                var (assembly, signature) = ctx.Left;
                var bodyStatements = ctx.Right;
                var body = bodyStatements[assembly];
                return Emitter.CreateGetMethod(signature, body);
            });

        return (projectGetComponentComponentsBodyThunk, getComponentMethodThunks);
    }

    private static IEnumerable<ComponentModel> ComponentWithAssembly(
        (((IAssemblySymbol, INamedTypeSymbol), INamedTypeSymbol), INamedTypeSymbol) context,
        CancellationToken _)
    {
        var (((candidate, componentsInterface), routeAttribute), renderModeAttribute) = context;
        var module = candidate.Modules.Single();

        var componentCollector = new ComponentCollector(componentsInterface, routeAttribute, renderModeAttribute);
        componentCollector.Visit(module.GlobalNamespace);

        foreach (var item in componentCollector.Components!)
        {
            yield return item;
        }
    }

    private ComponentModel CreateComponentModel((((ISymbol?, INamedTypeSymbol), INamedTypeSymbol), INamedTypeSymbol) context, CancellationToken token)
    {
        var (((component, _), routeAttribute), renderModeAttribute) = context;
        return ComponentModel.FromType((INamedTypeSymbol)component!, routeAttribute, renderModeAttribute);
    }

    private bool IsComponent((ISymbol? candidate, INamedTypeSymbol componentInterface) tuple)
    {
        return tuple.candidate is INamedTypeSymbol componentType &&
            ComponentCollector.IsComponent(componentType, tuple.componentInterface);
    }

    private bool FilterAssemblies((IAssemblySymbol assembly, IAssemblySymbol componentsAssembly) context)
    {
        var (assembly, componentsAssembly) = context;
        if (assembly.Name.StartsWith("System.", StringComparison.Ordinal) ||
            assembly.Name.StartsWith("Microsoft.", StringComparison.Ordinal))
        {
            // Filter out system assemblies as well as our components assemblies.
            return false;
        }

        if (assembly.Modules.Skip(1).Any())
        {
            return false;
        }
        var module = assembly.Modules.SingleOrDefault();
        if (module == null)
        {
            return false;
        }

        foreach (var refIdentity in module.ReferencedAssemblies)
        {
            if (refIdentity == componentsAssembly.Identity)
            {
                return true;
            }
        }

        return false;
    }
}

public class ComponentCollector : SymbolVisitor
{
    public ComponentCollector(INamedTypeSymbol componentsInterface, INamedTypeSymbol routeAttribute, INamedTypeSymbol renderModeAttribute)
    {
        if (componentsInterface is null)
        {
            throw new ArgumentNullException(nameof(componentsInterface));
        }

        if (routeAttribute is null)
        {
            throw new ArgumentNullException(nameof(routeAttribute));
        }

        ComponentsInterface = componentsInterface;
        RouteAttribute = routeAttribute;
        RenderModeAttribute = renderModeAttribute;
    }

    public List<ComponentModel>? Components { get; set; }

    public INamedTypeSymbol ComponentsInterface { get; set; }

    public INamedTypeSymbol RouteAttribute { get; set; }

    public INamedTypeSymbol RenderModeAttribute { get; set; }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            member.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        if (IsComponent(symbol, ComponentsInterface))
        {
            Components ??= new();
            Components.Add(ComponentModel.FromType(symbol, RouteAttribute, RenderModeAttribute));
        }
    }

    internal static bool IsComponent(INamedTypeSymbol candidate, INamedTypeSymbol componentInterface)
    {
        if (candidate.TypeKind != TypeKind.Class ||
            candidate.IsAbstract ||
            candidate.IsAnonymousType ||
            candidate.DeclaredAccessibility != Accessibility.Public ||
            string.Equals(candidate.Name, "_Imports", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var t in candidate.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(t, componentInterface))
            {
                return true;
            }
        }

        return false;
    }
}

public class ComponentModel
{
    public ComponentModel(INamedTypeSymbol component, ImmutableArray<AttributeData> routes, AttributeData? renderMode)
    {
        Component = component;
        Routes = routes;
        RenderMode = renderMode;
    }

    public bool IsPage => Routes.Length > 0;

    public INamedTypeSymbol Component { get; }
    public ImmutableArray<AttributeData> Routes { get; }
    public AttributeData? RenderMode { get; }

    internal static ComponentModel FromType(INamedTypeSymbol component, INamedTypeSymbol routeAttribute, INamedTypeSymbol renderModeAttribute)
    {
        var attributes = component!.GetAttributes();

        var routes = new List<AttributeData>();
        AttributeData? renderMode = null;

        for (var i = 0; i < attributes.Length; i++)
        {
            var attribute = attributes[i];
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, routeAttribute))
            {
                routes.Add(attribute);
            }
            else if (WellKnownTypes.IsSubClassOf(attribute.AttributeClass, renderModeAttribute))
            {
                renderMode = attribute;
            }
        }

        return new ComponentModel(component, routes.ToImmutableArray(), renderMode);
    }
}
