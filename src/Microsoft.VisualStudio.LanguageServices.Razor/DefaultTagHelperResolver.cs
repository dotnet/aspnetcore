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
                var client = await RazorLanguageServiceClientFactory.CreateAsync(Workspace, CancellationToken.None);
                if (client == null)
                {
                    // The OOP host is turned off, so let's do this in process.
                    var resolver = new CodeAnalysis.Razor.DefaultTagHelperResolver(designTime: true);
                    var result =  await resolver.GetTagHelpersAsync(project, assemblyNameFilters, CancellationToken.None).ConfigureAwait(false);
                    return result;
                }

                using (var session = await client.CreateSessionAsync(project.Solution))
                {
                    var result = await session.InvokeAsync<TagHelperResolutionResult>("GetTagHelpersAsync", new object[] { project.Id.Id, "Foo", assemblyNameFilters, }).ConfigureAwait(false);

                    // Per https://github.com/dotnet/roslyn/issues/12770 - there's currently no support for documentation in the OOP host
                    // until that's available we add the documentation on the VS side by looking up each symbol again.
                    var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
                    AddXmlDocumentation(compilation, result.Descriptors);

                    return result;
                }
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

        private void AddXmlDocumentation(Compilation compilation, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            for (var i = 0; i < tagHelpers.Count; i++)
            {
                var tagHelper = tagHelpers[i];
                tagHelper.DesignTimeDescriptor = tagHelper.DesignTimeDescriptor ?? new TagHelperDesignTimeDescriptor();

                var symbol = compilation.GetTypeByMetadataName(tagHelper.TypeName);
                if (symbol != null)
                {
                    var xml = symbol.GetDocumentationCommentXml();
                    if (!string.IsNullOrEmpty(xml))
                    {
                        var documentation = new XmlMemberDocumentation(xml);
                        tagHelper.DesignTimeDescriptor.Summary = documentation.GetSummary();
                        tagHelper.DesignTimeDescriptor.Remarks = documentation.GetRemarks();
                    }

                    foreach (var attribute in tagHelper.Attributes)
                    {
                        attribute.DesignTimeDescriptor = attribute.DesignTimeDescriptor ?? new TagHelperAttributeDesignTimeDescriptor();

                        var attributeSymbol = symbol.GetMembers(attribute.PropertyName).FirstOrDefault();
                        if (attributeSymbol != null)
                        {
                            xml = attributeSymbol.GetDocumentationCommentXml();
                            if (!string.IsNullOrEmpty(xml))
                            {
                                var documentation = new XmlMemberDocumentation(xml);
                                tagHelper.DesignTimeDescriptor.Summary = documentation.GetSummary();
                                tagHelper.DesignTimeDescriptor.Remarks = documentation.GetRemarks();
                            }
                        }
                    }
                }
            }
        }

        private IVsActivityLog GetActivityLog()
        {
            var services = (IServiceProvider)Services;
            return services.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
        }
    }
}
