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

            var symbols = BlazorSymbols.Create(compilation);

            var types = new List<INamedTypeSymbol>();
            var visitor = new ComponentTypeVisitor(symbols, types);

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
                var descriptor = CreateDescriptor(symbols, type);
                context.Results.Add(descriptor);

                foreach (var childContent in descriptor.GetChildContentProperties())
                {
                    // Synthesize a separate tag helper for each child content property that's declared.
                    context.Results.Add(CreateChildContentDescriptor(symbols, descriptor, childContent));
                }
            }
        }

        private Compilation WithMetadataImportOptionsAll(Compilation compilation)
        {
            var newCompilationOptions = (CSharpCompilationOptions)WithMetadataImportOptionsMethodInfo
                .Invoke(compilation.Options, new object[] { /* All */ (byte)2 });
            return compilation.WithOptions(newCompilationOptions);
        }

        private TagHelperDescriptor CreateDescriptor(BlazorSymbols symbols, INamedTypeSymbol type)
        {
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

            foreach (var property in GetProperties(symbols, type))
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

                    if (property.kind == PropertyKind.ChildContent)
                    {
                        pb.Metadata.Add(BlazorMetadata.Component.ChildContentKey, bool.TrueString);
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

        private TagHelperDescriptor CreateChildContentDescriptor(BlazorSymbols symbols, TagHelperDescriptor component, BoundAttributeDescriptor attribute)
        {
            var typeName = component.GetTypeName() + "." + attribute.Name;
            var assemblyName = component.AssemblyName;

            var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.ChildContent.TagHelperKind, typeName, assemblyName);
            builder.SetTypeName(typeName);

            // This opts out this 'component' tag helper for any processing that's specific to the default
            // Razor ITagHelper runtime.
            builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.ChildContent.RuntimeName;

            // Opt out of processing as a component. We'll process this specially as part of the component's body.
            builder.Metadata[BlazorMetadata.SpecialKindKey] = BlazorMetadata.ChildContent.TagHelperKind;

            var xml = attribute.Documentation;
            if (!string.IsNullOrEmpty(xml))
            {
                builder.Documentation = xml;
            }

            // Child content matches the property name, but only as a direct child of the component.
            builder.TagMatchingRule(r =>
            {
                r.TagName = attribute.Name;
                r.ParentTag = component.TagMatchingRules.First().TagName;
            });

            if (attribute.IsParameterizedChildContentProperty())
            {
                // For child content attributes with a parameter, synthesize an attribute that allows you to name
                // the parameter.
                builder.BindAttribute(b =>
                {
                    b.Name = "Context";
                    b.TypeName = typeof(string).FullName;
                    b.Documentation = string.Format(Resources.ChildContentParameterName_Documentation, attribute.Name);
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
        private IEnumerable<(IPropertySymbol property, PropertyKind kind)> GetProperties(BlazorSymbols symbols, INamedTypeSymbol type)
        {
            var properties = new Dictionary<string, (IPropertySymbol, PropertyKind)>(StringComparer.Ordinal);
            do
            {
                if (type == symbols.BlazorComponent)
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

                    if (!property.GetAttributes().Any(a => a.AttributeClass == symbols.ParameterAttribute))
                    {
                        // Does not have [Parameter]
                        kind = PropertyKind.Ignored;
                    }

                    if (kind == PropertyKind.Default && property.Type.TypeKind == TypeKind.Enum)
                    {
                        kind = PropertyKind.Enum;
                    }

                    if (kind == PropertyKind.Default && property.Type == symbols.RenderFragment)
                    {
                        kind = PropertyKind.ChildContent;
                    }

                    if (kind == PropertyKind.Default &&
                        property.Type is INamedTypeSymbol namedType &&
                        namedType.IsGenericType &&
                        namedType.ConstructedFrom == symbols.RenderFragmentOfT)
                    {
                        kind = PropertyKind.ChildContent;
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
            ChildContent,
            Delegate,
        }

        private class BlazorSymbols
        {
            public static BlazorSymbols Create(Compilation compilation)
            {
                var symbols = new BlazorSymbols();
                symbols.BlazorComponent = compilation.GetTypeByMetadataName(BlazorApi.BlazorComponent.MetadataName);
                if (symbols.BlazorComponent == null)
                {
                    // No definition for BlazorComponent, nothing to do.
                    return null;
                }

                symbols.IComponent = compilation.GetTypeByMetadataName(BlazorApi.IComponent.MetadataName);
                if (symbols.IComponent == null)
                {
                    // No definition for IComponent, nothing to do.
                    return null;
                }

                symbols.ParameterAttribute = compilation.GetTypeByMetadataName(BlazorApi.ParameterAttribute.MetadataName);
                if (symbols.ParameterAttribute == null)
                {
                    // No definition for [Parameter], nothing to do.
                    return null;
                }

                symbols.RenderFragment = compilation.GetTypeByMetadataName(BlazorApi.RenderFragment.MetadataName);
                if (symbols.RenderFragment == null)
                {
                    // No definition for RenderFragment, nothing to do.
                }

                symbols.RenderFragmentOfT = compilation.GetTypeByMetadataName(BlazorApi.RenderFragmentOfT.MetadataName);
                if (symbols.RenderFragmentOfT == null)
                {
                    // No definition for RenderFragment, nothing to do.
                }

                return symbols;
            }

            private BlazorSymbols()
            {
            }

            public INamedTypeSymbol BlazorComponent { get; private set; }

            public INamedTypeSymbol IComponent { get; private set; }

            public INamedTypeSymbol ParameterAttribute { get; private set; }

            public INamedTypeSymbol RenderFragment { get; private set; }

            public INamedTypeSymbol RenderFragmentOfT { get; private set; }
        }

        private class ComponentTypeVisitor : SymbolVisitor
        {
            private readonly BlazorSymbols _symbols;
            private readonly List<INamedTypeSymbol> _results;

            public ComponentTypeVisitor(BlazorSymbols symbols, List<INamedTypeSymbol> results)
            {
                _symbols = symbols;
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
                if (_symbols == null)
                {
                    return false;
                }

                return
                    symbol.DeclaredAccessibility == Accessibility.Public &&
                    !symbol.IsAbstract &&
                    !symbol.IsGenericType &&
                    symbol.AllInterfaces.Contains(_symbols.IComponent);
            }
        }
    }
}