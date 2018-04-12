// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class TestTagHelperResolver : TagHelperResolver
    {
        public TaskCompletionSource<TagHelperResolutionResult> CompletionSource { get; set; }

        public IList<TagHelperDescriptor> TagHelpers { get; } = new List<TagHelperDescriptor>();

        public override Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, CancellationToken cancellationToken = default)
        {
            if (CompletionSource == null)
            {
                return Task.FromResult(new TagHelperResolutionResult(TagHelpers.ToArray(), Array.Empty<RazorDiagnostic>()));
            }
            else
            {
                return CompletionSource.Task;
            }
        }
    }
}
