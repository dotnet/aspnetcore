// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal class OOPTagHelperResolver : TagHelperResolver
    {
        private readonly DefaultTagHelperResolver _defaultResolver;
        private readonly RazorProjectEngineFactoryService _engineFactory;
        private readonly ErrorReporter _errorReporter;
        private readonly Workspace _workspace;

        public OOPTagHelperResolver(RazorProjectEngineFactoryService engineFactory, ErrorReporter errorReporter, Workspace workspace)
        {
            if (engineFactory == null)
            {
                throw new ArgumentNullException(nameof(engineFactory));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _engineFactory = engineFactory;
            _errorReporter = errorReporter;
            _workspace = workspace;

            _defaultResolver = new DefaultTagHelperResolver(_engineFactory);
        }

        public override async Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, CancellationToken cancellationToken = default)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (project.Configuration == null || project.WorkspaceProject == null)
            {
                return TagHelperResolutionResult.Empty;
            }

            // Not every custom factory supports the OOP host. Our priority system should work like this:
            //
            // 1. Use custom factory out of process
            // 2. Use custom factory in process
            // 3. Use fallback factory in process
            //
            // Calling into RazorTemplateEngineFactoryService.Create will accomplish #2 and #3 in one step.
            var factory = _engineFactory.FindSerializableFactory(project);

            try
            {
                TagHelperResolutionResult result = null;
                if (factory != null)
                {
                    result = await ResolveTagHelpersOutOfProcessAsync(factory, project);
                }

                if (result == null)
                {
                    // Was unable to get tag helpers OOP, fallback to default behavior.
                    result = await ResolveTagHelpersInProcessAsync(project);
                }

                return result;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    Resources.FormatUnexpectedException(
                        typeof(DefaultTagHelperResolver).FullName,
                        nameof(GetTagHelpersAsync)),
                    exception);
            }
        }

        protected virtual async Task<TagHelperResolutionResult> ResolveTagHelpersOutOfProcessAsync(IProjectEngineFactory factory, ProjectSnapshot project)
        {
            // We're being overly defensive here because the OOP host can return null for the client/session/operation
            // when it's disconnected (user stops the process).
            //
            // This will change in the future to an easier to consume API but for VS RTM this is what we have.
            try
            {
                var client = await RazorLanguageServiceClientFactory.CreateAsync(_workspace, CancellationToken.None);
                if (client != null)
                {
                    using (var session = await client.CreateSessionAsync(project.WorkspaceProject.Solution))
                    {
                        if (session != null)
                        {
                            var args = new object[]
                            {
                                Serialize(project),
                                factory == null ? null : factory.GetType().AssemblyQualifiedName,
                            };

                            var json = await session.InvokeAsync<JObject>("GetTagHelpersAsync", args, CancellationToken.None).ConfigureAwait(false);
                            return Deserialize(json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // We silence exceptions from the OOP host because we don't want to bring down VS for an OOP failure.
                // We will retry all failures in process anyway, so if there's a real problem that isn't unique to OOP
                // then it will report a crash in VS.
                _errorReporter.ReportError(ex, project);
            }

            return null;
        }

        protected virtual Task<TagHelperResolutionResult> ResolveTagHelpersInProcessAsync(ProjectSnapshot project)
        {
            return _defaultResolver.GetTagHelpersAsync(project);
        }

        private JObject Serialize(ProjectSnapshot snapshot)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.RegisterRazorConverters();

            return JObject.FromObject(snapshot, serializer);
        }

        private TagHelperResolutionResult Deserialize(JObject jsonObject)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.RegisterRazorConverters();

            using (var reader = jsonObject.CreateReader())
            {
                return serializer.Deserialize<TagHelperResolutionResult>(reader);
            }
        }
    }
}
