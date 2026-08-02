// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Internal;

[assembly: MetadataUpdateHandler(typeof(HtmlAttributePropertyHelper))]

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal sealed class HtmlAttributePropertyHelper
{
    private static readonly ConcurrentDictionary<Type, HtmlAttributePropertyHelper[]> ReflectionCache =
        new ConcurrentDictionary<Type, HtmlAttributePropertyHelper[]>();
    private readonly PropertyHelper _propertyHelper;

    public HtmlAttributePropertyHelper(PropertyHelper propertyHelper)
    {
        _propertyHelper = propertyHelper;
        Name = propertyHelper.Name is string name ? name.Replace('_', '-') : null;
    }

    /// <summary>
    /// Part of <see cref="MetadataUpdateHandlerAttribute"/> contract.
    /// </summary>
    public static void ClearCache(Type[] _)
    {
        ReflectionCache.Clear();
    }

    public string Name { get; }

    public static HtmlAttributePropertyHelper[] GetProperties(Type type)
    {
        if (!ReflectionCache.TryGetValue(type, out var result))
        {
            var propertyHelpers = PropertyHelper.GetProperties(type, cache: null);
            result = new HtmlAttributePropertyHelper[propertyHelpers.Length];
            for (var i = 0; i < propertyHelpers.Length; i++)
            {
                result[i] = new(propertyHelpers[i]);
            }

            ReflectionCache[type] = result;
        }

        return result;
    }

    internal object GetValue(object instance) => _propertyHelper.GetValue(instance);
}
