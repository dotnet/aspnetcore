// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        // Hack for testability. The view component visitor will normally just no op if we're not referencing
        // an appropriate version of MVC.
        internal bool ForceEnableViewComponentDiscovery { get; set; }

        public override async Task<TagHelperResolutionResult> GetTagHelpersAsync(Project project, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var result = GetTagHelpers(compilation);
            return result;
        }

        // Internal for testing
        internal TagHelperResolutionResult GetTagHelpers(Compilation compilation)
        {
            var descriptors = new List<TagHelperDescriptor>();

            var providers = new ITagHelperDescriptorProvider[]
            {
                new DefaultTagHelperDescriptorProvider() { DesignTime = true, },
                new ViewComponentTagHelperDescriptorProvider() { ForceEnabled = ForceEnableViewComponentDiscovery },
            };

            var results = new List<TagHelperDescriptor>();
            var context = TagHelperDescriptorProviderContext.Create(results);
            context.SetCompilation(compilation);

            for (var i = 0; i < providers.Length; i++)
            {
                var provider = providers[i];
                provider.Execute(context);
            }

            var diagnostics = new List<RazorDiagnostic>();
            var resolutionResult = new TagHelperResolutionResult(results, diagnostics);

            return resolutionResult;
        }
    }
}
