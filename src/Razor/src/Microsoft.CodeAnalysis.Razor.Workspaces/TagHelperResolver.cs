// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    internal abstract class TagHelperResolver : ILanguageService
    {
        public abstract Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, CancellationToken cancellationToken = default);

        protected virtual async Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, RazorProjectEngine engine)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (project.WorkspaceProject == null)
            {
                return TagHelperResolutionResult.Empty;
            }

            var providers = engine.Engine.Features.OfType<ITagHelperDescriptorProvider>().ToArray();
            if (providers.Length == 0)
            {
                return TagHelperResolutionResult.Empty;
            }

            var results = new List<TagHelperDescriptor>();
            var context = TagHelperDescriptorProviderContext.Create(results);
            context.ExcludeHidden = true;
            context.IncludeDocumentation = true;

            var compilation = await project.WorkspaceProject.GetCompilationAsync().ConfigureAwait(false);
            if (CompilationTagHelperFeature.IsValidCompilation(compilation))
            {
                context.SetCompilation(compilation);
            }

            for (var i = 0; i < providers.Length; i++)
            {
                var provider = providers[i];
                provider.Execute(context);
            }

            return new TagHelperResolutionResult(results, Array.Empty<RazorDiagnostic>());
        }
    }
}
