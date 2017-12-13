// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal class OOPTagHelperResolver : TagHelperResolver
    {
        private readonly DefaultTagHelperResolver _defaultResolver;
        private readonly Workspace _workspace;

        public OOPTagHelperResolver(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _workspace = workspace;
            _defaultResolver = new DefaultTagHelperResolver();
        }

        public override async Task<TagHelperResolutionResult> GetTagHelpersAsync(Project project, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            try
            {
                TagHelperResolutionResult result = null;

                // We're being defensive here because the OOP host can return null for the client/session/operation
                // when it's disconnected (user stops the process).
                var client = await RazorLanguageServiceClientFactory.CreateAsync(_workspace, cancellationToken);
                if (client != null)
                {
                    using (var session = await client.CreateSessionAsync(project.Solution))
                    {
                        if (session != null)
                        {
                            var jsonObject = await session.InvokeAsync<JObject>(
                                "GetTagHelpersAsync",
                                new object[] { project.Id.Id, "Foo", },
                                cancellationToken).ConfigureAwait(false);

                            result = GetTagHelperResolutionResult(jsonObject);
                        }
                    }
                }

                if (result == null)
                {
                    // Was unable to get tag helpers OOP, fallback to default behavior.
                    result = await _defaultResolver.GetTagHelpersAsync(project, cancellationToken);
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
    }
}
