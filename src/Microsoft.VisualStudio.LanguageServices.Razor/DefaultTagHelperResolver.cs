// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(ITagHelperResolver))]
    internal class DefaultTagHelperResolver : ITagHelperResolver
    {
        [Import]
        public VisualStudioWorkspace Workspace { get; set; }

        [Import]
        public SVsServiceProvider Services { get; set; }

        public async Task<TagHelperResolutionResult> GetTagHelpersAsync(Project project, IEnumerable<string> assemblyNameFilters)
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
                            result = await session.InvokeAsync<TagHelperResolutionResult>(
                                "GetTagHelpersAsync",
                                new object[] { project.Id.Id, "Foo", assemblyNameFilters, }).ConfigureAwait(false);

                            if (result != null)
                            {
                                // Per https://github.com/dotnet/roslyn/issues/12770 - there's currently no support for documentation in the OOP host
                                // until that's available we add the documentation on the VS side by looking up each symbol again.
                                var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
                                var documentedTagHelpers = GetDocumentedTagHelpers(compilation, result.Descriptors);
                                var documentedResult = new TagHelperResolutionResult(documentedTagHelpers, result.Diagnostics);

                                return documentedResult;
                            }
                        }
                    }
                }

                // The OOP host is turned off, so let's do this in process.
                var resolver = new CodeAnalysis.Razor.DefaultTagHelperResolver(designTime: true);
                result = await resolver.GetTagHelpersAsync(project, assemblyNameFilters, CancellationToken.None).ConfigureAwait(false);
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

        private IReadOnlyList<TagHelperDescriptor> GetDocumentedTagHelpers(Compilation compilation, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            var documentedTagHelpers = new List<TagHelperDescriptor>();
            for (var i = 0; i < tagHelpers.Count; i++)
            {
                var tagHelper = tagHelpers[i];

                if (tagHelper.Documentation != null)
                {
                    documentedTagHelpers.Add(tagHelper);
                    continue;
                }

                var typeName = tagHelper.Metadata[ITagHelperDescriptorBuilder.TypeNameKey];
                var symbol = compilation.GetTypeByMetadataName(typeName);
                if (symbol != null)
                {
                    var tagHelperBuilder = ShallowCopy(typeName, tagHelper);
                    var xml = symbol.GetDocumentationCommentXml();

                    if (!string.IsNullOrEmpty(xml))
                    {
                        tagHelperBuilder.Documentation(xml);
                    }

                    foreach (var attribute in tagHelper.BoundAttributes)
                    {
                        var propertyName = attribute.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey];

                        var resolvedAttribute = attribute;
                        var attributeSymbol = symbol.GetMembers(propertyName).FirstOrDefault();
                        if (attributeSymbol != null)
                        {
                            xml = attributeSymbol.GetDocumentationCommentXml();
                            if (!string.IsNullOrEmpty(xml))
                            {
                                var attributeBuilder = ShallowCopy(typeName, resolvedAttribute);
                                attributeBuilder.Documentation(xml);
                                resolvedAttribute = attributeBuilder.Build();
                            }
                        }

                        tagHelperBuilder.BindAttribute(resolvedAttribute);
                    }

                    tagHelper = tagHelperBuilder.Build();
                }

                documentedTagHelpers.Add(tagHelper);
            }

            return documentedTagHelpers;
        }

        private ITagHelperBoundAttributeDescriptorBuilder ShallowCopy(string tagHelperTypeName, BoundAttributeDescriptor attribute)
        {
            var builder = ITagHelperBoundAttributeDescriptorBuilder.Create(tagHelperTypeName);

            if (attribute.IsEnum)
            {
                builder.AsEnum();
            }

            if (attribute.IndexerNamePrefix != null)
            {
                builder.AsDictionary(attribute.IndexerNamePrefix, attribute.IndexerTypeName);
            }

            builder.Name(attribute.Name);
            builder.TypeName(attribute.TypeName);

            var propertyName = attribute.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey];
            builder.PropertyName(propertyName);

            foreach (var metadata in attribute.Metadata)
            {
                builder.AddMetadata(metadata.Key, metadata.Value);
            }

            foreach (var diagnostic in attribute.Diagnostics)
            {
                builder.AddDiagnostic(diagnostic);
            }

            return builder;
        }

        private ITagHelperDescriptorBuilder ShallowCopy(string tagHelperTypeName, TagHelperDescriptor tagHelper)
        {
            var builder = ITagHelperDescriptorBuilder.Create(tagHelperTypeName, tagHelper.AssemblyName);

            foreach (var rule in tagHelper.TagMatchingRules)
            {
                builder.TagMatchingRule(rule);
            }

            if (tagHelper.AllowedChildTags != null)
            {
                foreach (var allowedChild in tagHelper.AllowedChildTags)
                {
                    builder.AllowChildTag(allowedChild);
                }
            }

            builder.TagOutputHint(tagHelper.TagOutputHint);

            foreach (var metadata in tagHelper.Metadata)
            {
                builder.AddMetadata(metadata.Key, metadata.Value);
            }

            foreach (var diagnostic in tagHelper.Diagnostics)
            {
                builder.AddDiagnostic(diagnostic);
            }

            return builder;
        }

        private IVsActivityLog GetActivityLog()
        {
            var services = (IServiceProvider)Services;
            return services.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
        }
    }
}
