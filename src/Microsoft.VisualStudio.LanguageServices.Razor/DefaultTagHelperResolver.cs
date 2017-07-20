// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(ITagHelperResolver))]
    internal class DefaultTagHelperResolver : ITagHelperResolver
    {
        [Import]
        public VisualStudioWorkspace Workspace { get; set; }

        [Import]
        public SVsServiceProvider Services { get; set; }

        public async Task<TagHelperResolutionResult> GetTagHelpersAsync(Project project)
        {
            try
            {
                TagHelperResolutionResult result;

                // We're being overly defensive here because the OOP host can return null for the client/session/operation
                // when it's disconnected (user stops the process).
                //
                // This will change in the future to an easier to consume API but for VS RTM this is what we have.
                var client = await RazorLanguageServiceClientFactory.CreateAsync(Workspace, CancellationToken.None);
                if (client != null)
                {
                    using (var session = await client.CreateSessionAsync(project.Solution))
                    {
                        if (session != null)
                        {
                            var jsonObject = await session.InvokeAsync<JObject>(
                                "GetTagHelpersAsync",
                                new object[] { project.Id.Id, "Foo", }).ConfigureAwait(false);

                            result = GetTagHelperResolutionResult(jsonObject);

                            if (result != null)
                            {
                                return result;
                            }
                        }
                    }
                }

                // The OOP host is turned off, so let's do this in process.
                var compilation = await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
                result = GetTagHelpers(compilation, designTime: true);
                return result;
            }
            catch (Exception exception)
            {
                var log = GetActivityLog();
                if (log != null)
                {
                    var hr = log.LogEntry(
                        (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                        "Razor Language Services",
                        $"Error discovering TagHelpers:{Environment.NewLine}{exception}");
                    ErrorHandler.ThrowOnFailure(hr);
                }

                throw new RazorLanguageServiceException(
                    typeof(DefaultTagHelperResolver).FullName,
                    nameof(GetTagHelpersAsync),
                    exception);
            }
        }

        private TagHelperResolutionResult GetTagHelpers(Compilation compilation, bool designTime)
        {
            var descriptors = new List<TagHelperDescriptor>();

            var providers = new ITagHelperDescriptorProvider[]
            {
                new DefaultTagHelperDescriptorProvider() { DesignTime = designTime, },
                new ViewComponentTagHelperDescriptorProvider(),
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

        private TagHelperResolutionResult GetTagHelperResolutionResult(JObject jsonObject)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(TagHelperDescriptorJsonConverter.Instance);
            serializer.Converters.Add(RazorDiagnosticJsonConverter.Instance);

            using (var reader = jsonObject.CreateReader())
            {
                return serializer.Deserialize<TagHelperResolutionResult>(reader);
            }
        }

        private IVsActivityLog GetActivityLog()
        {
            var services = (IServiceProvider)Services;
            return services.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
        }
    }
}
