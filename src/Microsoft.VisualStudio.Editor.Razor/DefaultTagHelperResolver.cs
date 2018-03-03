// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        private readonly RazorProjectEngineFactoryService _engineFactory;

        public DefaultTagHelperResolver(RazorProjectEngineFactoryService engineFactory)
        {
            if (engineFactory == null)
            {
                throw new ArgumentNullException(nameof(engineFactory));
            }

            _engineFactory = engineFactory;
        }

        public override Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, CancellationToken cancellationToken = default)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (project.Configuration == null || project.WorkspaceProject == null)
            {
                return Task.FromResult(TagHelperResolutionResult.Empty);
            }

            var engine = _engineFactory.Create(project, RazorProjectFileSystem.Empty, b => 
            {
                b.Features.Add(new DefaultTagHelperDescriptorProvider() { DesignTime = true, });
            });
            return GetTagHelpersAsync(project, engine);
        }
    }
}
