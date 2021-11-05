// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.CodeAnalysis.Razor;

internal class ComponentTagHelperDescriptorProvider : RazorEngineFeatureBase, ITagHelperDescriptorProvider
{
    private static readonly SymbolDisplayFormat FullNameTypeDisplayFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions & (~SymbolDisplayMiscellaneousOptions.UseSpecialTypes));

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

        var symbols = ComponentSymbols.Create(compilation);

        var types = new List<INamedTypeSymbol>();
        var visitor = new ComponentTypeVisitor(symbols, types);

        var targetAssembly = context.Items.GetTargetAssembly();
        if (targetAssembly is not null)
        {
            visitor.Visit(targetAssembly.GlobalNamespace);
        }
        else
        {
            visitor.Visit(compilation.Assembly.GlobalNamespace);
            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    visitor.Visit(assembly.GlobalNamespace);
                }
            }
        }

        for (var i = 0; i < types.Count; i++)
        {
            var type = types[i];

            // Components have very simple matching rules.
            // 1. The type name (short) matches the tag name.
            // 2. The fully qualified name matches the tag name.
            var shortNameMatchingDescriptor = CreateShortNameMatchingDescriptor(symbols, type);
            context.Results.Add(shortNameMatchingDescriptor);
            var fullyQualifiedNameMatchingDescriptor = CreateFullyQualifiedNameMatchingDescriptor(symbols, type);
            context.Results.Add(fullyQualifiedNameMatchingDescriptor);

            foreach (var childContent in shortNameMatchingDescriptor.GetChildContentProperties())
            {
                // Synthesize a separate tag helper for each child content property that's declared.
                context.Results.Add(CreateChildContentDescriptor(symbols, shortNameMatchingDescriptor, childContent));
                context.Results.Add(CreateChildContentDescriptor(symbols, fullyQualifiedNameMatchingDescriptor, childContent));
            }
        }
    }

    private TagHelperDescriptor CreateShortNameMatchingDescriptor(ComponentSymbols symbols, INamedTypeSymbol type)
    {
        var builder = CreateDescriptorBuilder(symbols, type);
        builder.TagMatchingRule(r =>
        {
            r.TagName = type.Name;
        });

        return builder.Build();
    }

    private TagHelperDescriptor CreateFullyQualifiedNameMatchingDescriptor(ComponentSymbols symbols, INamedTypeSymbol type)
    {
        var builder = CreateDescriptorBuilder(symbols, type);
        var containingNamespace = type.ContainingNamespace.ToDisplayString();
        var fullName = $"{containingNamespace}.{type.Name}";
        builder.TagMatchingRule(r =>
        {
            r.TagName = fullName;
        });
        builder.Metadata[ComponentMetadata.Component.NameMatchKey] = ComponentMetadata.Component.FullyQualifiedNameMatch;

        return builder.Build();
    }

    private TagHelperDescriptorBuilder CreateDescriptorBuilder(ComponentSymbols symbols, INamedTypeSymbol type)
    {
        var typeName = type.ToDisplayString(FullNameTypeDisplayFormat);
        var assemblyName = type.ContainingAssembly.Identity.Name;

        var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, typeName, assemblyName);
        builder.SetTypeName(typeName);
        builder.CaseSensitive = true;

        // This opts out this 'component' tag helper for any processing that's specific to the default
        // Razor ITagHelper runtime.
        builder.Metadata[TagHelperMetadata.Runtime.Name] = ComponentMetadata.Component.RuntimeName;

        if (type.IsGenericType)
        {
            builder.Metadata[ComponentMetadata.Component.GenericTypedKey] = bool.TrueString;

            var cascadeGenericTypeAttributes = type
                .GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.CascadingTypeParameterAttribute))
                .Select(attribute => attribute.ConstructorArguments.FirstOrDefault().Value as string)
                .ToList();

            for (var i = 0; i < type.TypeArguments.Length; i++)
            {
                var typeParameter = type.TypeArguments[i] as ITypeParameterSymbol;
                if (typeParameter != null)
                {
                    var cascade = cascadeGenericTypeAttributes.Contains(typeParameter.Name, StringComparer.Ordinal);
                    CreateTypeParameterProperty(builder, typeParameter, cascade);
                }
            }
        }

        var xml = type.GetDocumentationCommentXml();
        if (!string.IsNullOrEmpty(xml))
        {
            builder.Documentation = xml;
        }

        foreach (var property in GetProperties(symbols, type))
        {
            if (property.kind == PropertyKind.Ignored)
            {
                continue;
            }

            CreateProperty(builder, property.property, property.kind);
        }

        if (builder.BoundAttributes.Any(a => a.IsParameterizedChildContentProperty()) &&
            !builder.BoundAttributes.Any(a => string.Equals(a.Name, ComponentMetadata.ChildContent.ParameterAttributeName, StringComparison.OrdinalIgnoreCase)))
        {
            // If we have any parameterized child content parameters, synthesize a 'Context' parameter to be
            // able to set the variable name (for all child content). If the developer defined a 'Context' parameter
            // already, then theirs wins.
            CreateContextParameter(builder, childContentName: null);
        }

        return builder;
    }

    private void CreateProperty(TagHelperDescriptorBuilder builder, IPropertySymbol property, PropertyKind kind)
    {
        builder.BindAttribute(pb =>
        {
            pb.Name = property.Name;
            pb.TypeName = property.Type.ToDisplayString(FullNameTypeDisplayFormat);
            pb.SetPropertyName(property.Name);
            pb.IsEditorRequired = property.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == "Microsoft.AspNetCore.Components.EditorRequiredAttribute");

            if (kind == PropertyKind.Enum)
            {
                pb.IsEnum = true;
            }

            if (kind == PropertyKind.ChildContent)
            {
                pb.Metadata.Add(ComponentMetadata.Component.ChildContentKey, bool.TrueString);
            }

            if (kind == PropertyKind.EventCallback)
            {
                pb.Metadata.Add(ComponentMetadata.Component.EventCallbackKey, bool.TrueString);
            }

            if (kind == PropertyKind.Delegate)
            {
                pb.Metadata.Add(ComponentMetadata.Component.DelegateSignatureKey, bool.TrueString);
            }

            if (HasTypeParameter(property.Type))
            {
                pb.Metadata.Add(ComponentMetadata.Component.GenericTypedKey, bool.TrueString);
            }

            var xml = property.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xml))
            {
                pb.Documentation = xml;
            }
        });

        bool HasTypeParameter(ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol)
            {
                return true;
            }

            // We need to check for cases like:
            // [Parameter] public List<T> MyProperty { get; set; }
            // AND
            // [Parameter] public List<string> MyProperty { get; set; }
            //
            // We need to inspect the type arguments to tell the difference between a property that
            // uses the containing class' type parameter(s) and a vanilla usage of generic types like
            // List<> and Dictionary<,>
            //
            // Since we need to handle cases like RenderFragment<List<T>>, this check must be recursive.
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var typeArguments = namedType.TypeArguments;
                for (var i = 0; i < typeArguments.Length; i++)
                {
                    if (HasTypeParameter(typeArguments[i]))
                    {
                        return true;
                    }
                }

                // Another case to handle - if the type being inspected is a nested type
                // inside a generic containing class. The common usage for this would be a case
                // where a generic templated component defines a 'context' nested class.
                if (namedType.ContainingType != null && HasTypeParameter(namedType.ContainingType))
                {
                    return true;
                }
            }
            // Also check for cases like:
            // [Parameter] public T[] MyProperty { get; set; }
            else if (type is IArrayTypeSymbol array && HasTypeParameter(array.ElementType))
            {
                return true;
            }

            return false;
        }
    }

    private void CreateTypeParameterProperty(TagHelperDescriptorBuilder builder, ITypeParameterSymbol typeParameter, bool cascade)
    {
        builder.BindAttribute(pb =>
        {
            pb.DisplayName = typeParameter.Name;
            pb.Name = typeParameter.Name;
            pb.TypeName = typeof(Type).FullName;
            pb.SetPropertyName(typeParameter.Name);

            pb.Metadata[ComponentMetadata.Component.TypeParameterKey] = bool.TrueString;
            pb.Metadata[ComponentMetadata.Component.TypeParameterIsCascadingKey] = cascade.ToString();

            // Type constraints (like "Image" or "Foo") are stored indepenently of
            // things like constructor constraints and not null constraints in the
            // type parameter so we create a single string representation of all the constraints
            // here.
            var constraintString = new StringBuilder();
            if (typeParameter.ConstraintTypes.Any())
            {
                constraintString.Append(string.Join(", ", typeParameter.ConstraintTypes.Select(t => t.Name)));
            }
            if (typeParameter.HasConstructorConstraint)
            {
                constraintString.Append(", new()");
            }
            if (typeParameter.HasNotNullConstraint)
            {
                constraintString.Append(", notnull");
            }
            if (typeParameter.HasReferenceTypeConstraint)
            {
                constraintString.Append(", class");
            }
            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                constraintString.Append(", unmanaged");
            }
            if (typeParameter.HasValueTypeConstraint)
            {
                constraintString.Append(", struct");
            }

            if (constraintString.Length > 0)
            {
                constraintString.Insert(0, "where " + typeParameter.Name + " : ");
                pb.Metadata[ComponentMetadata.Component.TypeParameterConstraintsKey] = constraintString.ToString();
            }

            pb.Documentation = string.Format(CultureInfo.InvariantCulture, ComponentResources.ComponentTypeParameter_Documentation, typeParameter.Name, builder.Name);
        });
    }

    private TagHelperDescriptor CreateChildContentDescriptor(ComponentSymbols symbols, TagHelperDescriptor component, BoundAttributeDescriptor attribute)
    {
        var typeName = component.GetTypeName() + "." + attribute.Name;
        var assemblyName = component.AssemblyName;

        var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.ChildContent.TagHelperKind, typeName, assemblyName);
        builder.SetTypeName(typeName);
        builder.CaseSensitive = true;

        // This opts out this 'component' tag helper for any processing that's specific to the default
        // Razor ITagHelper runtime.
        builder.Metadata[TagHelperMetadata.Runtime.Name] = ComponentMetadata.ChildContent.RuntimeName;

        // Opt out of processing as a component. We'll process this specially as part of the component's body.
        builder.Metadata[ComponentMetadata.SpecialKindKey] = ComponentMetadata.ChildContent.TagHelperKind;

        var xml = attribute.Documentation;
        if (!string.IsNullOrEmpty(xml))
        {
            builder.Documentation = xml;
        }

        // Child content matches the property name, but only as a direct child of the component.
        builder.TagMatchingRule(r =>
        {
            r.TagName = attribute.Name;
            r.ParentTag = component.TagMatchingRules[0].TagName;
        });

        if (attribute.IsParameterizedChildContentProperty())
        {
            // For child content attributes with a parameter, synthesize an attribute that allows you to name
            // the parameter.
            CreateContextParameter(builder, attribute.Name);
        }

        if (component.IsComponentFullyQualifiedNameMatch())
        {
            builder.Metadata[ComponentMetadata.Component.NameMatchKey] = ComponentMetadata.Component.FullyQualifiedNameMatch;
        }

        var descriptor = builder.Build();

        return descriptor;
    }

    private void CreateContextParameter(TagHelperDescriptorBuilder builder, string childContentName)
    {
        builder.BindAttribute(b =>
        {
            b.Name = ComponentMetadata.ChildContent.ParameterAttributeName;
            b.TypeName = typeof(string).FullName;
            b.Metadata.Add(ComponentMetadata.Component.ChildContentParameterNameKey, bool.TrueString);
            b.Metadata.Add(TagHelperMetadata.Common.PropertyName, b.Name);

            if (childContentName == null)
            {
                b.Documentation = ComponentResources.ChildContentParameterName_TopLevelDocumentation;
            }
            else
            {
                b.Documentation = string.Format(CultureInfo.InvariantCulture, ComponentResources.ChildContentParameterName_Documentation, childContentName);
            }
        });
    }

    // Does a walk up the inheritance chain to determine the set of parameters by using
    // a dictionary keyed on property name.
    //
    // We consider parameters to be defined by properties satisfying all of the following:
    // - are public
    // - are visible (not shadowed)
    // - have the [Parameter] attribute
    // - have a setter, even if private
    // - are not indexers
    private IEnumerable<(IPropertySymbol property, PropertyKind kind)> GetProperties(ComponentSymbols symbols, INamedTypeSymbol type)
    {
        var properties = new Dictionary<string, (IPropertySymbol, PropertyKind)>(StringComparer.Ordinal);
        do
        {
            if (SymbolEqualityComparer.Default.Equals(type, symbols.ComponentBase))
            {
                // The ComponentBase base class doesn't have any [Parameter].
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
                if (property.DeclaredAccessibility != Accessibility.Public)
                {
                    // Not public
                    kind = PropertyKind.Ignored;
                }

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
                else if (property.SetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    // No public setter
                    kind = PropertyKind.Ignored;
                }

                if (property.IsStatic)
                {
                    kind = PropertyKind.Ignored;
                }

                if (!property.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.ParameterAttribute)))
                {
                    if (property.IsOverride)
                    {
                        // This property does not contain [Parameter] attribute but it was overridden. Don't ignore it for now.
                        // We can ignore it if the base class does not contains a [Parameter] as well.
                        continue;
                    }

                    // Does not have [Parameter]
                    kind = PropertyKind.Ignored;
                }

                if (kind == PropertyKind.Default && property.Type.TypeKind == TypeKind.Enum)
                {
                    kind = PropertyKind.Enum;
                }

                if (kind == PropertyKind.Default && SymbolEqualityComparer.Default.Equals(property.Type, symbols.RenderFragment))
                {
                    kind = PropertyKind.ChildContent;
                }

                if (kind == PropertyKind.Default &&
                    property.Type is INamedTypeSymbol namedType &&
                    namedType.IsGenericType &&
                    SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, symbols.RenderFragmentOfT))
                {
                    kind = PropertyKind.ChildContent;
                }

                if (kind == PropertyKind.Default && SymbolEqualityComparer.Default.Equals(property.Type, symbols.EventCallback))
                {
                    kind = PropertyKind.EventCallback;
                }

                if (kind == PropertyKind.Default &&
                    property.Type is INamedTypeSymbol namedType2 &&
                    namedType2.IsGenericType &&
                    SymbolEqualityComparer.Default.Equals(namedType2.ConstructedFrom, symbols.EventCallbackOfT))
                {
                    kind = PropertyKind.EventCallback;
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
        EventCallback,
    }

    private class ComponentSymbols
    {
        public static ComponentSymbols Create(Compilation compilation)
        {
            // We find a bunch of important and fundamental types here that are needed to discover
            // components. If one of these isn't defined then we just bail, because the results will
            // be unpredictable.
            var symbols = new ComponentSymbols();

            symbols.ComponentBase = compilation.GetTypeByMetadataName(ComponentsApi.ComponentBase.MetadataName);
            if (symbols.ComponentBase == null)
            {
                // No definition for ComponentBase, nothing to do.
                return null;
            }

            symbols.IComponent = compilation.GetTypeByMetadataName(ComponentsApi.IComponent.MetadataName);
            if (symbols.IComponent == null)
            {
                // No definition for IComponent, nothing to do.
                return null;
            }

            symbols.ParameterAttribute = compilation.GetTypeByMetadataName(ComponentsApi.ParameterAttribute.MetadataName);
            if (symbols.ParameterAttribute == null)
            {
                // No definition for [Parameter], nothing to do.
                return null;
            }

            symbols.RenderFragment = compilation.GetTypeByMetadataName(ComponentsApi.RenderFragment.MetadataName);
            if (symbols.RenderFragment == null)
            {
                // No definition for RenderFragment, nothing to do.
                return null;
            }

            symbols.RenderFragmentOfT = compilation.GetTypeByMetadataName(ComponentsApi.RenderFragmentOfT.MetadataName);
            if (symbols.RenderFragmentOfT == null)
            {
                // No definition for RenderFragment<T>, nothing to do.
                return null;
            }

            symbols.EventCallback = compilation.GetTypeByMetadataName(ComponentsApi.EventCallback.MetadataName);
            if (symbols.EventCallback == null)
            {
                // No definition for EventCallback, nothing to do.
                return null;
            }

            symbols.EventCallbackOfT = compilation.GetTypeByMetadataName(ComponentsApi.EventCallbackOfT.MetadataName);
            if (symbols.EventCallbackOfT == null)
            {
                // No definition for EventCallback<T>, nothing to do.
                return null;
            }

            symbols.CascadingTypeParameterAttribute = compilation.GetTypeByMetadataName(ComponentsApi.CascadingTypeParameterAttribute.MetadataName);
            if (symbols.CascadingTypeParameterAttribute == null)
            {
                // No definition for [CascadingTypeParameter]. For back-compat, just don't activate this feature.
            }

            return symbols;
        }

        private ComponentSymbols()
        {
        }

        public INamedTypeSymbol ComponentBase { get; private set; }

        public INamedTypeSymbol IComponent { get; private set; }

        public INamedTypeSymbol ParameterAttribute { get; private set; }

        public INamedTypeSymbol RenderFragment { get; private set; }

        public INamedTypeSymbol RenderFragmentOfT { get; private set; }

        public INamedTypeSymbol EventCallback { get; private set; }

        public INamedTypeSymbol EventCallbackOfT { get; private set; }

        public INamedTypeSymbol CascadingTypeParameterAttribute { get; private set; }
    }

    private class ComponentTypeVisitor : SymbolVisitor
    {
        private readonly ComponentSymbols _symbols;
        private readonly List<INamedTypeSymbol> _results;

        public ComponentTypeVisitor(ComponentSymbols symbols, List<INamedTypeSymbol> results)
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

            var isComponent = ComponentDetectionConventions.IsComponent(symbol, _symbols.IComponent);
            return isComponent;
        }
    }
}
