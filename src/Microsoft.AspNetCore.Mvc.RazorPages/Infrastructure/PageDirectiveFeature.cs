// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Evolution;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public static class PageDirectiveFeature
    {
        public static bool TryGetRouteTemplate(RazorProjectItem projectItem, out string template)
        {
            const string PageDirective = "@page";

            string content;
            using (var streamReader = new StreamReader(projectItem.Read()))
            {
                content = streamReader.ReadToEnd();
            }

            if (content.StartsWith(PageDirective, StringComparison.Ordinal))
            {
                var newLineIndex = content.IndexOf(Environment.NewLine, PageDirective.Length);
                template = content.Substring(PageDirective.Length, newLineIndex - PageDirective.Length).Trim();
                return true;
            }

            template = null;
            return false;
        }
    }
}
