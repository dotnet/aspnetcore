// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorEngineBuilderExtensions
    {
        public static IRazorEngineBuilder AddTagHelpers(this IRazorEngineBuilder builder, params TagHelperDescriptor[] tagHelpers)
        {
            return AddTagHelpers(builder, (IEnumerable<TagHelperDescriptor>)tagHelpers);
        }

        public static IRazorEngineBuilder AddTagHelpers(this IRazorEngineBuilder builder, IEnumerable<TagHelperDescriptor> tagHelpers)
        {
            var feature = (TestTagHelperFeature)builder.Features.OfType<ITagHelperFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = new TestTagHelperFeature();
                builder.Features.Add(feature);
            }

            feature.TagHelpers.AddRange(tagHelpers);
            return builder;
        }
    }
}
