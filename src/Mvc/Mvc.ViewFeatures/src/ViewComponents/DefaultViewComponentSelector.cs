// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Default implementation of <see cref="IViewComponentSelector"/>.
/// </summary>
public class DefaultViewComponentSelector : IViewComponentSelector
{
    private readonly IViewComponentDescriptorCollectionProvider _descriptorProvider;

    private ViewComponentDescriptorCache _cache;

    /// <summary>
    /// Creates a new <see cref="DefaultViewComponentSelector"/>.
    /// </summary>
    /// <param name="descriptorProvider">The <see cref="IViewComponentDescriptorCollectionProvider"/>.</param>
    public DefaultViewComponentSelector(IViewComponentDescriptorCollectionProvider descriptorProvider)
    {
        _descriptorProvider = descriptorProvider;
    }

    /// <inheritdoc />
    public ViewComponentDescriptor SelectComponent(string componentName)
    {
        ArgumentNullException.ThrowIfNull(componentName);

        var collection = _descriptorProvider.ViewComponents;
        if (_cache == null || _cache.Version != collection.Version)
        {
            _cache = new ViewComponentDescriptorCache(collection);
        }

        // ViewComponent names can either be fully-qualified, or refer to the 'short-name'. If the provided
        // name contains a '.' - then it's a fully-qualified name.
        if (componentName.Contains('.'))
        {
            return _cache.SelectByFullName(componentName);
        }
        else
        {
            return _cache.SelectByShortName(componentName);
        }
    }

    private sealed class ViewComponentDescriptorCache
    {
        private readonly ILookup<string, ViewComponentDescriptor> _lookupByShortName;
        private readonly ILookup<string, ViewComponentDescriptor> _lookupByFullName;

        public ViewComponentDescriptorCache(ViewComponentDescriptorCollection collection)
        {
            Version = collection.Version;

            _lookupByShortName = collection.Items.ToLookup(c => c.ShortName, c => c);
            _lookupByFullName = collection.Items.ToLookup(c => c.FullName, c => c);
        }

        public int Version { get; }

        public ViewComponentDescriptor SelectByShortName(string name)
        {
            return Select(_lookupByShortName, name);
        }

        public ViewComponentDescriptor SelectByFullName(string name)
        {
            return Select(_lookupByFullName, name);
        }

        private static ViewComponentDescriptor Select(
            ILookup<string, ViewComponentDescriptor> candidates,
            string name)
        {
            var matches = candidates[name];

            var count = matches.Count();
            if (count == 0)
            {
                return null;
            }
            else if (count == 1)
            {
                return matches.Single();
            }
            else
            {
                var matchedTypes = new List<string>();
                foreach (var candidate in matches)
                {
                    matchedTypes.Add(Resources.FormatViewComponent_AmbiguousTypeMatch_Item(
                        candidate.TypeInfo.FullName,
                        candidate.FullName));
                }

                var typeNames = string.Join(Environment.NewLine, matchedTypes);
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_AmbiguousTypeMatch(name, Environment.NewLine, typeNames));
            }
        }
    }
}
