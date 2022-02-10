// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

/// <summary>
/// An <see cref="IApplicationFeatureProvider"/> for the <see cref="TagHelperFeature"/>.
/// </summary>
public class TagHelperFeatureProvider : IApplicationFeatureProvider<TagHelperFeature>
{
    /// <inheritdoc/>
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, TagHelperFeature feature)
    {
        foreach (var part in parts)
        {
            if (IncludePart(part) && part is IApplicationPartTypeProvider typeProvider)
            {
                foreach (var type in typeProvider.Types)
                {
                    var typeInfo = type.GetTypeInfo();
                    if (IncludeType(typeInfo) && !feature.TagHelpers.Contains(typeInfo))
                    {
                        feature.TagHelpers.Add(typeInfo);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Include a part.
    /// </summary>
    /// <param name="part">The part to include.</param>
    /// <returns>True if included.</returns>
    protected virtual bool IncludePart(ApplicationPart part) => true;

    /// <summary>
    /// Include a type.
    /// </summary>
    /// <param name="type">The type to include.</param>
    /// <returns>True if included.</returns>
    protected virtual bool IncludeType(TypeInfo type)
    {
        // We don't need to check visibility here, that's handled by the type provider.
        return
            typeof(ITagHelper).GetTypeInfo().IsAssignableFrom(type) &&
            !type.IsAbstract &&
            !type.IsGenericType;
    }
}
