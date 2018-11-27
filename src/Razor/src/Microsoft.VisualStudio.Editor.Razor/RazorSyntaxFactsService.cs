// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public abstract class RazorSyntaxFactsService : ILanguageService
    {
        public abstract IReadOnlyList<ClassifiedSpan> GetClassifiedSpans(RazorSyntaxTree syntaxTree);

        public abstract IReadOnlyList<TagHelperSpan> GetTagHelperSpans(RazorSyntaxTree syntaxTree);
    }
}
