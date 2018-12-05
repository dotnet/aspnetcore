// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class RemoteTagHelperResolver : TagHelperResolver
    {
        private readonly static RazorConfiguration DefaultConfiguration = FallbackRazorConfiguration.MVC_2_0;

        private readonly IFallbackProjectEngineFactory _fallbackFactory;

        public RemoteTagHelperResolver(IFallbackProjectEngineFactory fallbackFactory)
        {
            if (fallbackFactory == null)
            {
                throw new ArgumentNullException(nameof(fallbackFactory));
            }

            _fallbackFactory = fallbackFactory;
        }

        public override Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, string factoryTypeName, CancellationToken cancellationToken = default)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (project.Configuration == null || project.WorkspaceProject == null)
            {
                return Task.FromResult(TagHelperResolutionResult.Empty);
            }

            var engine = CreateProjectEngine(project, factoryTypeName);
            return GetTagHelpersAsync(project, engine);
        }

        internal RazorProjectEngine CreateProjectEngine(ProjectSnapshot project, string factoryTypeName)
        {
            // This section is really similar to the code DefaultProjectEngineFactoryService
            // but with a few differences that are significant in the remote scenario
            //
            // Most notably, we are going to find the Tag Helpers using a compilation, and we have
            // no editor settings.
            //
            // The default configuration currently matches MVC-2.0. Beyond MVC-2.0 we added SDK support for 
            // properly detecting project versions, so that's a good version to assume when we can't find a
            // configuration.
            var configuration = project?.Configuration ?? DefaultConfiguration;

            // If there's no factory to handle the configuration then fall back to a very basic configuration.
            //
            // This will stop a crash from happening in this case (misconfigured project), but will still make
            // it obvious to the user that something is wrong.
            var factory = CreateFactory(configuration, factoryTypeName) ?? _fallbackFactory;
            return factory.Create(configuration, RazorProjectFileSystem.Empty, b => { });
        }

        private IProjectEngineFactory CreateFactory(RazorConfiguration configuration, string factoryTypeName)
        {
            if (factoryTypeName == null)
            {
                return null;
            }

            return (IProjectEngineFactory)Activator.CreateInstance(Type.GetType(factoryTypeName, throwOnError: true));
        }
    }
}
