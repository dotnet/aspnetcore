// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class BindTagHelperDescriptorProvider : ITagHelperDescriptorProvider
    {
        // Run after the component tag helper provider, because we need to see the results.
        public int Order { get; set; } = 1000;

        public RazorEngine Engine { get; set; }

        public void Execute(TagHelperDescriptorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // This provider returns tag helper information for 'bind' which doesn't necessarily
            // map to any real component. Bind behaviors more like a macro, which can map a single LValue to
            // both a 'value' attribute and a 'value changed' attribute.
            //
            // User types: 
            //      <input type="text" bind="@FirstName"/>
            //
            // We generate:
            //      <input type="text" 
            //          value="@BindMethods.GetValue(FirstName)" 
            //          onchange="@BindMethods.SetValue(__value => FirstName = __value, FirstName)"/>
            //
            // This isn't very different from code the user could write themselves - thus the pronouncement
            // that bind is very much like a macro.
            //
            // A lot of the value that provide in this case is that the associations between the
            // elements, and the attributes aren't straightforward.
            //
            // For instance on <input type="text" /> we need to listen to 'value' and 'onchange',
            // but on <input type="checked" we need to listen to 'checked' and 'onchange'.
            //
            // We handle a few different cases here:
            //
            //  1.  When given an attribute like **anywhere**'bind-value-onchange="@FirstName"' we will
            //      generate the 'value' attribute and 'onchange' attribute. 
            //
            //      We don't do any transformation or inference for this case, because the developer has
            //      told us exactly what to do. This is the *full* form of bind, and should support any
            //      combination of element, component, and attributes.
            //
            //      This is the most general case, and is implemented with a built-in tag helper that applies
            //      to everything, and binds to a dictionary of attributes that start with bind-.
            //
            //  2.  We also support cases like 'bind-value="@FirstName"' where we will generate the 'value'
            //      attribute and another attribute based for a changed handler based on the metadata.
            //
            //     These mappings are provided by attributes that tell us what attributes, suffixes, and
            //      elements to map.
            //
            //  3.  When given an attribute like 'bind="@FirstName"' we will generate a value and change
            //      attribute solely based on the context. We need the context of an HTML tag to know
            //      what attributes to generate.
            //
            //      Similar to case #2, this should 'just work' from the users point of view. We expect
            //      using this syntax most frequently with input elements.
            //
            //      These mappings are also provided by attributes. Primarily these are used by <input />
            //      and so we have a special case for input elements and their type attributes.
            //
            //  4.  For components, we have a bit of a special case. We can infer a syntax that matches
            //      case #2 based on property names. So if a component provides both 'Value' and 'ValueChanged'
            //      we will turn that into an instance of bind.
            //
            // So case #1 here is the most general case. Case #2 and #3 are data-driven based on attribute data
            // we have. Case #4 is data-driven based on component definitions.
            //
            // We provide a good set of attributes that map to the HTML dom. This set is user extensible.
            var compilation = context.GetCompilation();
            if (compilation == null)
            {
                return;
            }

            var bindMethods = compilation.GetTypeByMetadataName(ComponentsApi.BindMethods.FullTypeName);
            if (bindMethods == null)
            {
                // If we can't find BindMethods, then just bail. We won't be able to compile the
                // generated code anyway.
                return;
            }

            // Tag Helper defintion for case #1. This is the most general case.
            context.Results.Add(CreateFallbackBindTagHelper());

            // For case #2 & #3 we have a whole bunch of attribute entries on BindMethods that we can use
            // to data-drive the definitions of these tag helpers.
            var elementBindData = GetElementBindData(compilation);

            // Case #2 & #3
            foreach (var tagHelper in CreateElementBindTagHelpers(elementBindData))
            {
                context.Results.Add(tagHelper);
            }

            // For case #4 we look at the tag helpers that were already created corresponding to components
            // and pattern match on properties.
            foreach (var tagHelper in CreateComponentBindTagHelpers(context.Results))
            {
                context.Results.Add(tagHelper);
            }
        }

        private TagHelperDescriptor CreateFallbackBindTagHelper()
        {
            var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.Bind.TagHelperKind, "Bind", ComponentsApi.AssemblyName);
            builder.Documentation = Resources.BindTagHelper_Fallback_Documentation;

            builder.Metadata.Add(BlazorMetadata.SpecialKindKey, BlazorMetadata.Bind.TagHelperKind);
            builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.Bind.RuntimeName;
            builder.Metadata[BlazorMetadata.Bind.FallbackKey] = bool.TrueString;

            // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
            // a C# property will crash trying to create the toolips.
            builder.SetTypeName("Microsoft.AspNetCore.Components.Bind");

            builder.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.Attribute(attribute =>
                {
                    attribute.Name = "bind-";
                    attribute.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
                });
            });

            builder.BindAttribute(attribute =>
            {
                attribute.Documentation = Resources.BindTagHelper_Fallback_Documentation;

                attribute.Name = "bind-...";
                attribute.AsDictionary("bind-", typeof(object).FullName);

                // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                // a C# property will crash trying to create the toolips.
                attribute.SetPropertyName("Bind");
                attribute.TypeName = "System.Collections.Generic.Dictionary<string, object>";
            });

            builder.BindAttribute(attribute =>
            {
                attribute.Documentation = Resources.BindTagHelper_Fallback_Format_Documentation;

                attribute.Name = "format-...";
                attribute.AsDictionary("format-", typeof(string).FullName);

                // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                // a C# property will crash trying to create the toolips.
                attribute.SetPropertyName("Format");
                attribute.TypeName = "System.Collections.Generic.Dictionary<string, string>";
            });

            return builder.Build();
        }

        private List<ElementBindData> GetElementBindData(Compilation compilation)
        {
            var bindElement = compilation.GetTypeByMetadataName(ComponentsApi.BindElementAttribute.FullTypeName);
            var bindInputElement = compilation.GetTypeByMetadataName(ComponentsApi.BindInputElementAttribute.FullTypeName);

            if (bindElement == null || bindInputElement == null)
            {
                // This won't likely happen, but just in case.
                return new List<ElementBindData>();
            }

            var types = new List<INamedTypeSymbol>();
            var visitor = new BindElementDataVisitor(types);

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

            var results = new List<ElementBindData>();

            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var attributes = type.GetAttributes();

                // Not handling duplicates here for now since we're the primary ones extending this.
                // If we see users adding to the set of 'bind' constructs we will want to add deduplication
                // and potentially diagnostics.
                for (var j = 0; j < attributes.Length; j++)
                {
                    var attribute = attributes[j];

                    if (attribute.AttributeClass == bindElement)
                    {
                        results.Add(new ElementBindData(
                            type.ContainingAssembly.Name,
                            type.ToDisplayString(),
                            (string)attribute.ConstructorArguments[0].Value,
                            null,
                            (string)attribute.ConstructorArguments[1].Value,
                            (string)attribute.ConstructorArguments[2].Value,
                            (string)attribute.ConstructorArguments[3].Value));
                    }
                    else if (attribute.AttributeClass == bindInputElement)
                    {
                        results.Add(new ElementBindData(
                            type.ContainingAssembly.Name,
                            type.ToDisplayString(),
                            "input",
                            (string)attribute.ConstructorArguments[0].Value,
                            (string)attribute.ConstructorArguments[1].Value,
                            (string)attribute.ConstructorArguments[2].Value,
                            (string)attribute.ConstructorArguments[3].Value));
                    }
                }
            }

            return results;
        }
        private List<TagHelperDescriptor> CreateElementBindTagHelpers(List<ElementBindData> data)
        {
            var results = new List<TagHelperDescriptor>();

            for (var i = 0; i < data.Count; i++)
            {
                var entry = data[i];

                var name = entry.Suffix == null ? "Bind" : "Bind_" + entry.Suffix;
                var attributeName = entry.Suffix == null ? "bind" : "bind-" + entry.Suffix;

                var formatName = entry.Suffix == null ? "Format_" + entry.ValueAttribute : "Format_" + entry.Suffix;
                var formatAttributeName = entry.Suffix == null ? "format-" + entry.ValueAttribute : "format-" + entry.Suffix;

                var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.Bind.TagHelperKind, name, entry.Assembly);
                builder.Documentation = string.Format(
                    Resources.BindTagHelper_Element_Documentation,
                    entry.ValueAttribute,
                    entry.ChangeAttribute);

                builder.Metadata.Add(BlazorMetadata.SpecialKindKey, BlazorMetadata.Bind.TagHelperKind);
                builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.Bind.RuntimeName;
                builder.Metadata[BlazorMetadata.Bind.ValueAttribute] = entry.ValueAttribute;
                builder.Metadata[BlazorMetadata.Bind.ChangeAttribute] = entry.ChangeAttribute;

                if (entry.TypeAttribute != null)
                {
                    // For entries that map to the <input /> element, we need to be able to know
                    // the difference between <input /> and <input type="text" .../> for which we
                    // want to use the same attributes.
                    //
                    // We provide a tag helper for <input /> that should match all input elements,
                    // but we only want it to be used when a more specific one is used.
                    // 
                    // Therefore we use this metadata to know which one is more specific when two
                    // tag helpers match.
                    builder.Metadata[BlazorMetadata.Bind.TypeAttribute] = entry.TypeAttribute;
                }

                // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
                // a C# property will crash trying to create the toolips.
                builder.SetTypeName(entry.TypeName);

                builder.TagMatchingRule(rule =>
                {
                    rule.TagName = entry.Element;
                    if (entry.TypeAttribute != null)
                    {
                        rule.Attribute(a =>
                        {
                            a.Name = "type";
                            a.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                            a.Value = entry.TypeAttribute;
                            a.ValueComparisonMode = RequiredAttributeDescriptor.ValueComparisonMode.FullMatch;
                        });
                    }
                    
                    rule.Attribute(a =>
                    {
                        a.Name = attributeName;
                        a.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                    });
                });

                builder.BindAttribute(a =>
                {
                    a.Documentation = string.Format(
                        Resources.BindTagHelper_Element_Documentation,
                        entry.ValueAttribute,
                        entry.ChangeAttribute);

                    a.Name = attributeName;
                    a.TypeName = typeof(object).FullName;

                    // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                    // a C# property will crash trying to create the toolips.
                    a.SetPropertyName(name);
                });

                builder.BindAttribute(attribute =>
                {
                    attribute.Documentation = string.Format(Resources.BindTagHelper_Element_Format_Documentation, attributeName);

                    attribute.Name = formatAttributeName;
                    attribute.TypeName = "System.String";

                    // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                    // a C# property will crash trying to create the toolips.
                    attribute.SetPropertyName(formatName);
                });

                results.Add(builder.Build());
            }

            return results;
        }

        private List<TagHelperDescriptor> CreateComponentBindTagHelpers(ICollection<TagHelperDescriptor> tagHelpers)
        {
            var results = new List<TagHelperDescriptor>();

            foreach (var tagHelper in tagHelpers)
            {
                if (!tagHelper.IsComponentTagHelper())
                {
                    continue;
                }

                // We want to create a 'bind' tag helper everywhere we see a pair of properties like `Foo`, `FooChanged`
                // where `FooChanged` is a delegate and `Foo` is not.
                //
                // The easiest way to figure this out without a lot of backtracking is to look for `FooChanged` and then
                // try to find a matching "Foo".
                for (var i = 0; i < tagHelper.BoundAttributes.Count; i++)
                {
                    var changeAttribute = tagHelper.BoundAttributes[i];
                    if (!changeAttribute.Name.EndsWith("Changed") || !changeAttribute.IsDelegateProperty())
                    {
                        continue;
                    }

                    BoundAttributeDescriptor valueAttribute = null;
                    var valueAttributeName = changeAttribute.Name.Substring(0, changeAttribute.Name.Length - "Changed".Length);
                    for (var j = 0; j < tagHelper.BoundAttributes.Count; j++)
                    {
                        if (tagHelper.BoundAttributes[j].Name == valueAttributeName && !tagHelper.BoundAttributes[j].IsDelegateProperty())
                        {
                            valueAttribute = tagHelper.BoundAttributes[j];
                            break;
                        }
                    }

                    if (valueAttribute == null)
                    {
                        // No matching attribute found.
                        continue;
                    }

                    var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.Bind.TagHelperKind, tagHelper.Name, tagHelper.AssemblyName);
                    builder.DisplayName = tagHelper.DisplayName;
                    builder.Documentation = string.Format(
                        Resources.BindTagHelper_Component_Documentation,
                        valueAttribute.Name,
                        changeAttribute.Name);

                    builder.Metadata.Add(BlazorMetadata.SpecialKindKey, BlazorMetadata.Bind.TagHelperKind);
                    builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.Bind.RuntimeName;
                    builder.Metadata[BlazorMetadata.Bind.ValueAttribute] = valueAttribute.Name;
                    builder.Metadata[BlazorMetadata.Bind.ChangeAttribute] = changeAttribute.Name;

                    // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                    // a C# property will crash trying to create the toolips.
                    builder.SetTypeName(tagHelper.GetTypeName());

                    // Match the component and attribute name
                    builder.TagMatchingRule(rule =>
                    {
                        rule.TagName = tagHelper.TagMatchingRules.Single().TagName;
                        rule.Attribute(attribute =>
                        {
                            attribute.Name = "bind-" + valueAttribute.Name;
                            attribute.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                        });
                    });

                    builder.BindAttribute(attribute =>
                    {
                        attribute.Documentation = string.Format(
                            Resources.BindTagHelper_Component_Documentation,
                            valueAttribute.Name,
                            changeAttribute.Name);

                        attribute.Name = "bind-" + valueAttribute.Name;
                        attribute.TypeName = valueAttribute.TypeName;
                        attribute.IsEnum = valueAttribute.IsEnum;

                        // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                        // a C# property will crash trying to create the toolips.
                        attribute.SetPropertyName(valueAttribute.GetPropertyName());
                    });

                    results.Add(builder.Build());
                }
            }

            return results;
        }

        private struct ElementBindData
        {
            public ElementBindData(
                string assembly,
                string typeName,
                string element,
                string typeAttribute,
                string suffix,
                string valueAttribute,
                string changeAttribute)
            {
                Assembly = assembly;
                TypeName = typeName;
                Element = element;
                TypeAttribute = typeAttribute;
                Suffix = suffix;
                ValueAttribute = valueAttribute;
                ChangeAttribute = changeAttribute;
            }

            public string Assembly { get; }
            public string TypeName { get; }
            public string Element { get; }
            public string TypeAttribute { get; }
            public string Suffix { get; }
            public string ValueAttribute { get; }
            public string ChangeAttribute { get; }
        }

        private class BindElementDataVisitor : SymbolVisitor
        {
            private List<INamedTypeSymbol> _results;

            public BindElementDataVisitor(List<INamedTypeSymbol> results)
            {
                _results = results;
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (symbol.Name == "BindAttributes" && symbol.DeclaredAccessibility == Accessibility.Public)
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
        }
    }
}
