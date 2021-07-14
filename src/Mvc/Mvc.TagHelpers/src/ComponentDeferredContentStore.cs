// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    internal static class ComponentDeferredContentStore
    {
        private static readonly object ContentStoreKey = new();

        internal static Dictionary<string, IHtmlContent> GetOrCreateContentStore(ViewContext viewContext)
        {
            if (!viewContext.Items.TryGetValue(ContentStoreKey, out var result))
            {
                result = new Dictionary<string, IHtmlContent>();
                viewContext.Items[ContentStoreKey] = result;
            }

            return (Dictionary<string, IHtmlContent>)result;
        }
    }
}
