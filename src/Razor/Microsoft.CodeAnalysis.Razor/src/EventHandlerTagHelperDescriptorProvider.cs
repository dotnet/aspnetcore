// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.CodeAnalysis.Razor;

internal class EventHandlerTagHelperDescriptorProvider : ITagHelperDescriptorProvider
{
    public int Order { get; set; }

    public RazorEngine Engine { get; set; }

    public void Execute(TagHelperDescriptorProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var compilation = context.GetCompilation();
        if (compilation == null)
        {
            return;
        }

        if (compilation.GetTypeByMetadataName(ComponentsApi.EventHandlerAttribute.FullTypeName) is not INamedTypeSymbol eventHandlerAttribute)
        {
            // If we can't find EventHandlerAttribute, then just bail. We won't discover anything.
            return;
        }

        var eventHandlerData = GetEventHandlerData(context, compilation, eventHandlerAttribute);

        foreach (var tagHelper in CreateEventHandlerTagHelpers(eventHandlerData))
        {
            context.Results.Add(tagHelper);
        }
    }

    private List<EventHandlerData> GetEventHandlerData(TagHelperDescriptorProviderContext context, Compilation compilation, INamedTypeSymbol eventHandlerAttribute)
    {
        var types = new List<INamedTypeSymbol>();
        var visitor = new EventHandlerDataVisitor(types);

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

        var results = new List<EventHandlerData>();

        for (var i = 0; i < types.Count; i++)
        {
            var type = types[i];
            var attributes = type.GetAttributes();

            // Not handling duplicates here for now since we're the primary ones extending this.
            // If we see users adding to the set of event handler constructs we will want to add deduplication
            // and potentially diagnostics.
            for (var j = 0; j < attributes.Length; j++)
            {
                var attribute = attributes[j];

                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, eventHandlerAttribute))
                {
                    var enablePreventDefault = false;
                    var enableStopPropagation = false;
                    if (attribute.ConstructorArguments.Length == 4)
                    {
                        enablePreventDefault = (bool)attribute.ConstructorArguments[2].Value;
                        enableStopPropagation = (bool)attribute.ConstructorArguments[3].Value;
                    }

                    results.Add(new EventHandlerData(
                        type.ContainingAssembly.Name,
                        type.ToDisplayString(),
                        (string)attribute.ConstructorArguments[0].Value,
                        (INamedTypeSymbol)attribute.ConstructorArguments[1].Value,
                        enablePreventDefault,
                        enableStopPropagation));
                }
            }
        }

        return results;
    }

    private List<TagHelperDescriptor> CreateEventHandlerTagHelpers(List<EventHandlerData> data)
    {
        var results = new List<TagHelperDescriptor>();

        for (var i = 0; i < data.Count; i++)
        {
            var entry = data[i];
            var attributeName = "@" + entry.Attribute;
            var eventArgType = entry.EventArgsType.ToDisplayString();

            var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.EventHandler.TagHelperKind, entry.Attribute, ComponentsApi.AssemblyName);
            builder.CaseSensitive = true;
            builder.Documentation = string.Format(
                CultureInfo.CurrentCulture,
                ComponentResources.EventHandlerTagHelper_Documentation,
                attributeName,
                eventArgType);

            builder.Metadata.Add(ComponentMetadata.SpecialKindKey, ComponentMetadata.EventHandler.TagHelperKind);
            builder.Metadata.Add(ComponentMetadata.EventHandler.EventArgsType, eventArgType);
            builder.Metadata.Add(TagHelperMetadata.Common.ClassifyAttributesOnly, bool.TrueString);
            builder.Metadata[TagHelperMetadata.Runtime.Name] = ComponentMetadata.EventHandler.RuntimeName;

            // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
            // a C# property will crash trying to create the tooltips.
            builder.SetTypeName(entry.TypeName);

            builder.TagMatchingRule(rule =>
            {
                rule.TagName = "*";

                rule.Attribute(a =>
                {
                    a.Name = attributeName;
                    a.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                    a.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
                });
            });

            if (entry.EnablePreventDefault)
            {
                builder.TagMatchingRule(rule =>
                {
                    rule.TagName = "*";

                    rule.Attribute(a =>
                    {
                        a.Name = attributeName + ":preventDefault";
                        a.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                        a.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
                    });
                });
            }

            if (entry.EnableStopPropagation)
            {
                builder.TagMatchingRule(rule =>
                {
                    rule.TagName = "*";

                    rule.Attribute(a =>
                    {
                        a.Name = attributeName + ":stopPropagation";
                        a.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                        a.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
                    });
                });
            }

            builder.BindAttribute(a =>
            {
                a.Documentation = string.Format(
                    CultureInfo.CurrentCulture,
                    ComponentResources.EventHandlerTagHelper_Documentation,
                    attributeName,
                    eventArgType);

                a.Name = attributeName;

                    // We want event handler directive attributes to default to C# context.
                    a.TypeName = $"Microsoft.AspNetCore.Components.EventCallback<{eventArgType}>";

                    // But make this weakly typed (don't type check) - delegates have their own type-checking
                    // logic that we don't want to interfere with.
                    a.Metadata.Add(ComponentMetadata.Component.WeaklyTypedKey, bool.TrueString);

                a.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;

                    // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                    // a C# property will crash trying to create the tooltips.
                    a.SetPropertyName(entry.Attribute);

                if (entry.EnablePreventDefault)
                {
                    a.BindAttributeParameter(parameter =>
                    {
                        parameter.Name = "preventDefault";
                        parameter.TypeName = typeof(bool).FullName;
                        parameter.Documentation = string.Format(
                            CultureInfo.CurrentCulture, ComponentResources.EventHandlerTagHelper_PreventDefault_Documentation, attributeName);

                        parameter.SetPropertyName("PreventDefault");
                    });
                }

                if (entry.EnableStopPropagation)
                {
                    a.BindAttributeParameter(parameter =>
                    {
                        parameter.Name = "stopPropagation";
                        parameter.TypeName = typeof(bool).FullName;
                        parameter.Documentation = string.Format(
                            CultureInfo.CurrentCulture, ComponentResources.EventHandlerTagHelper_StopPropagation_Documentation, attributeName);

                        parameter.SetPropertyName("StopPropagation");
                    });
                }
            });

            results.Add(builder.Build());
        }

        return results;
    }

    private struct EventHandlerData
    {
        public EventHandlerData(
            string assembly,
            string typeName,
            string element,
            INamedTypeSymbol eventArgsType,
            bool enablePreventDefault,
            bool enableStopPropagation)
        {
            Assembly = assembly;
            TypeName = typeName;
            Attribute = element;
            EventArgsType = eventArgsType;
            EnablePreventDefault = enablePreventDefault;
            EnableStopPropagation = enableStopPropagation;
        }

        public string Assembly { get; }

        public string TypeName { get; }

        public string Attribute { get; }

        public INamedTypeSymbol EventArgsType { get; }

        public bool EnablePreventDefault { get; }

        public bool EnableStopPropagation { get; }
    }

    private class EventHandlerDataVisitor : SymbolVisitor
    {
        private readonly List<INamedTypeSymbol> _results;

        public EventHandlerDataVisitor(List<INamedTypeSymbol> results)
        {
            _results = results;
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (symbol.Name == "EventHandlers" && symbol.DeclaredAccessibility == Accessibility.Public)
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
