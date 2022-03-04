// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    public class TagHelperFeatureProvider : IApplicationFeatureProvider<TagHelperFeature>
    {
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

        protected virtual bool IncludePart(ApplicationPart part) => true;

        protected virtual bool IncludeType(TypeInfo type)
        {
            // We don't need to check visibility here, that's handled by the type provider.
            return
                typeof(ITagHelper).GetTypeInfo().IsAssignableFrom(type) &&
                !type.IsAbstract &&
                !type.IsGenericType;
        }
    }
}
