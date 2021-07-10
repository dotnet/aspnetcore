// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    internal static class PrerenderingHelpers
    {
        private static readonly object PrerenderCacheKey = new();

        internal const string PrerenderedNameName = "name";

        internal static Dictionary<string, IHtmlContent> GetOrCreatePrerenderCache(ViewContext viewContext)
        {
            if (!viewContext.Items.TryGetValue(PrerenderCacheKey, out var result))
            {
                result = new Dictionary<string, IHtmlContent>();
                viewContext.Items[PrerenderCacheKey] = result;
            }

            return (Dictionary<string, IHtmlContent>)result;
        }
    }
}
