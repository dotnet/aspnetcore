// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

internal sealed class RazorCompiledItemFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
{
    private Dictionary<string, Type>? _hotReloadedViews;

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
    {
        foreach (var provider in parts.OfType<IRazorCompiledItemProvider>())
        {
            // Ensure parts do not specify views with differing cases. This is not supported
            // at runtime and we should flag at as such for precompiled views.
            var duplicates = provider.CompiledItems
                .GroupBy(i => i.Identifier, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicates != null)
            {
                var viewsDifferingInCase = string.Join(Environment.NewLine, duplicates.Select(d => d.Identifier));

                var message = string.Join(
                    Environment.NewLine,
                    Resources.RazorViewCompiler_ViewPathsDifferOnlyInCase,
                    viewsDifferingInCase);
                throw new InvalidOperationException(message);
            }

            foreach (var item in provider.CompiledItems)
            {
                var compiledItem = item;
                if (_hotReloadedViews is not null && _hotReloadedViews.TryGetValue(item.Identifier, out var hotReloadedType))
                {
                    // Determine if a hot reload update is available for this view.
                    compiledItem = new HotReloadRazorCompiledItem(item, hotReloadedType);
                }

                var descriptor = new CompiledViewDescriptor(compiledItem);
                feature.ViewDescriptors.Add(descriptor);
            }
        }
    }

    public void UpdateCache(Type[]? types)
    {
        if (types is null)
        {
            return;
        }

        foreach (var type in types)
        {
            // The Razor file has a [RazorCompiledItemMetadata("Identifier", "/Index.cshtml")]. We'll look it up.
            var metadataAttribute = type.GetCustomAttributes<RazorCompiledItemMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "Identifier");

            if (metadataAttribute is RazorCompiledItemMetadataAttribute identifierAttribute)
            {
                _hotReloadedViews ??= new(StringComparer.Ordinal);
                _hotReloadedViews[identifierAttribute.Value] = type;
            }
        }
    }

    private sealed class HotReloadRazorCompiledItem : RazorCompiledItem
    {
        private readonly RazorCompiledItem _previous;
        public HotReloadRazorCompiledItem(RazorCompiledItem previous, Type type)
        {
            _previous = previous;
            Type = type;
        }

        public override string Identifier => _previous.Identifier;
        public override string Kind => _previous.Kind;
        public override IReadOnlyList<object> Metadata => Type.GetCustomAttributes(inherit: true);
        public override Type Type { get; }
    }
}
