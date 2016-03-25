// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    /// <summary>
    /// Discovers tag helpers from a list of <see cref="ApplicationPart"/> instances.
    /// </summary>
    public class TagHelperFeatureProvider : IApplicationFeatureProvider<TagHelperFeature>
    {
        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, TagHelperFeature feature)
        {
            foreach (var type in parts.OfType<IApplicationPartTypeProvider>())
            {
                ProcessPart(type, feature);
            }
        }

        private static void ProcessPart(IApplicationPartTypeProvider part, TagHelperFeature feature)
        {
            foreach (var type in part.Types)
            {
                if (TagHelperConventions.IsTagHelper(type) && !feature.TagHelpers.Contains(type))
                {
                    feature.TagHelpers.Add(type);
                }
            }
        }
    }
}
