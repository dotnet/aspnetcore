// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public interface ITagHelperResolver
    {
        Task<TagHelperResolutionResult> GetTagHelpersAsync(Project project);
    }
}
