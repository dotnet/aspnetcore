// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Blazor.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class ComponentTagHelperDescriptorProvider : RazorEngineFeatureBase, ITagHelperDescriptorProvider
    {
        private static readonly SymbolDisplayFormat FullNameTypeDisplayFormat =
            SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
                .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions & (~SymbolDisplayMiscellaneousOptions.UseSpecialTypes));

        private static MethodInfo WithMetadataImportOptionsMethodInfo =
            typeof(CSharpCompilationOptions)
                .GetMethod("WithMetadataImportOptions", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public bool IncludeDocumentation { get; set; }

        public int Order { get; set; }

        public void Execute(TagHelperDescriptorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var compilation = context.GetCompilation();
            if (compilation == null)
            {
                // No compilation, nothing to do.
                return;
            }

            // We need to see private members too
            compilation = WithMetadataImportOptionsAll(compilation);

            var componentSymbol = compilation.GetTypeByMetadataName(BlazorApi.IComponent.MetadataName);
            if (componentSymbol == null)
            {
                // No definition for IComponent, nothing to do.
                return;
            }

            var parameterSymbol = compilation.GetTypeByMetadataName(BlazorApi.ParameterAttribute.FullTypeName);
            if (parameterSymbol == null)
            {
                // No definition for [Parameter], nothing to do.
                return;
            }

            var blazorComponentSymbol = compilation.GetTypeByMetadataName(BlazorApi.BlazorComponent.FullTypeName);
            if (blazorComponentSymbol == null)
            {
                // No definition for BlazorComponent, nothing to do.
                return;
            }

            var types = new List<INamedTypeSymbol>();
            var visitor = new ComponentTypeVisitor(componentSymbol, types);

            // Visit the primary output of this compilation, as well as all references.
            visitor.Visit(compilation.Assembly);
            foreach (var reference in compilation.References)
            {
                // We ignore .netmodules here - there really isn't a case where they are used by user code
                // even though the Roslyn APIs all support them.
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    visitor.Visit(assembly);
                }
            }

            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                context.Results.Add(CreateDescriptor(type, parameterSymbol, blazorComponentSymbol));
            }
        }

        private Compilation WithMetadataImportOptionsAll(Compilation compilation)
        {
            var newCompilationOptions = (CSharpCompilationOptions)WithMetadataImportOptionsMethodInfo
                .Invoke(compilation.Options, new object[] { /* All */ (byte)2 });
            return compilation.WithOptions(newCompilationOptions);
        }

        private TagHelperDescriptor CreateDescriptor(INamedTypeSymbol type, INamedTypeSymbol parameterSymbol, INamedTypeSymbol blazorComponentSymbol)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (parameterSymbol == null)
            {
                throw new ArgumentNullException(nameof(parameterSymbol));
            }

            var typeName = type.ToDisplayString(FullNameTypeDisplayFormat);
            var assemblyName = type.ContainingAssembly.Identity.Name;

            var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.Component.TagHelperKind, typeName, assemblyName);
            builder.SetTypeName(typeName);

            // This opts out this 'component' tag helper for any processing that's specific to the default
            // Razor ITagHelper runtime.
            builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.Component.RuntimeName;

            var xml = type.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xml))
            {
                builder.Documentation = xml;
            }

            // Components have very simple matching rules. The type name (short) matches the tag name.
            builder.TagMatchingRule(r => r.TagName = type.Name);

            foreach (var property in GetProperties(type, parameterSymbol, blazorComponentSymbol))
            {
                if (property.kind == PropertyKind.Ignored)
                {
                    continue;
                }

                builder.BindAttribute(pb =>
                {
                    pb.Name = property.property.Name;
                    pb.TypeName = property.property.Type.ToDisplayString(FullNameTypeDisplayFormat);
                    pb.SetPropertyName(property.property.Name);

                    if (property.kind == PropertyKind.Enum)
                    {
                        pb.IsEnum = true;
                    }

                    if (property.kind == PropertyKind.Delegate)
                    {
                        pb.Metadata.Add(BlazorMetadata.Component.DelegateSignatureKey, bool.TrueString);
                    }

                    xml = property.property.GetDocumentationCommentXml();
                    if (!string.IsNullOrEmpty(xml))
                    {
                        pb.Documentation = xml;
                    }
                });
            }

            var descriptor = builder.Build();

            return descriptor;
        }

        // Does a walk up the inheritance chain to determine the set of parameters by using
        // a dictionary keyed on property name.
        //
        // We consider parameters to be defined by properties satisfying all of the following:
        // - are visible (not shadowed)
        // - have the [Parameter] attribute
        // - have a setter, even if private
        // - are not indexers
        private IEnumerable<(IPropertySymbol property, PropertyKind kind)> GetProperties(INamedTypeSymbol type, INamedTypeSymbol parameterSymbol, INamedTypeSymbol blazorComponentSymbol)
        {
            var properties = new Dictionary<string, (IPropertySymbol, PropertyKind)>(StringComparer.Ordinal);
            do
            {
                if (type == blazorComponentSymbol)
                {
                    // The BlazorComponent base class doesn't have any [Parameter].
                    // Bail out now to avoid walking through its many members, plus the members
                    // of the System.Object base class.
                    break;
                }

                var members = type.GetMembers();
                for (var i = 0; i < members.Length; i++)
                {
                    var property = members[i] as IPropertySymbol;
                    if (property == null)
                    {
                        // Not a property
                        continue;
                    }

                    if (properties.ContainsKey(property.Name))
                    {
                        // Not visible
                        continue;
                    }

                    var kind = PropertyKind.Default;
                    if (property.Parameters.Length != 0)
                    {
                        // Indexer
                        kind = PropertyKind.Ignored;
                    }

                    if (property.SetMethod == null)
                    {
                        // No setter
                        kind = PropertyKind.Ignored;
                    }

                    if (property.IsStatic)
                    {
                        kind = PropertyKind.Ignored;
                    }

                    if (!property.GetAttributes().Any(a => a.AttributeClass == parameterSymbol))
                    {
                        // Does not have [Parameter]
                        kind = PropertyKind.Ignored;
                    }

                    if (kind == PropertyKind.Default && property.Type.TypeKind == TypeKind.Enum)
                    {
                        kind = PropertyKind.Enum;
                    }

                    if (kind == PropertyKind.Default && property.Type.TypeKind == TypeKind.Delegate)
                    {
                        kind = PropertyKind.Delegate;
                    }

                    properties.Add(property.Name, (property, kind));
                }

                type = type.BaseType;
            }
            while (type != null);

            return properties.Values;
        }

        private enum PropertyKind
        {
            Ignored,
            Default,
            Enum,
            Delegate,
        }

        private class ComponentTypeVisitor : SymbolVisitor
        {
            private INamedTypeSymbol _interface;
            private List<INamedTypeSymbol> _results;

            public ComponentTypeVisitor(INamedTypeSymbol @interface, List<INamedTypeSymbol> results)
            {
                _interface = @interface;
                _results = results;
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (IsComponent(symbol))
                {
                    _results.Add(symbol);
                }
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var member in symbol.GetMembers())
                {
                    Visit(member);
                }
            }

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                // This as a simple yet high-value optimization that excludes the vast majority of
                // assemblies that (by definition) can't contain a component.
                if (symbol.Name != null && !symbol.Name.StartsWith("System.", StringComparison.Ordinal))
                {
                    Visit(symbol.GlobalNamespace);
                }
            }

            internal bool IsComponent(INamedTypeSymbol symbol)
            {
                if (_interface == null)
                {
                    return false;
                }

                return
                    symbol.DeclaredAccessibility == Accessibility.Public &&
                    !symbol.IsAbstract &&
                    !symbol.IsGenericType &&
                    symbol.AllInterfaces.Contains(_interface);
            }
        }
    }
}