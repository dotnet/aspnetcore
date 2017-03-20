// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public abstract class RazorSyntaxFactsService
    {
        public abstract IReadOnlyList<ClassifiedSpan> GetClassifiedSpans(RazorSyntaxTree syntaxTree);

        public abstract IReadOnlyList<TagHelperSpan> GetTagHelperSpans(RazorSyntaxTree syntaxTree);
    }
}
