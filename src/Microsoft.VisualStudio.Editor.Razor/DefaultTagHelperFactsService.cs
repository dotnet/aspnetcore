// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(TagHelperFactsService))]
    internal class DefaultTagHelperFactsService : TagHelperFactsService
    {
        private readonly TagHelperFactsServiceInternal _tagHelperFactsService;

        [ImportingConstructor]
        public DefaultTagHelperFactsService(VisualStudioWorkspaceAccessor workspaceAccessor)
        {
            var razorLanguageServices = workspaceAccessor.Workspace.Services.GetLanguageServices(RazorLanguage.Name);
            _tagHelperFactsService = razorLanguageServices.GetRequiredService<TagHelperFactsServiceInternal>();
        }

        public override TagHelperBinding GetTagHelperBinding(
            TagHelperDocumentContext documentContext,
            string tagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string parentTag,
            bool parentIsTagHelper)
        {
            return _tagHelperFactsService.GetTagHelperBinding(documentContext, tagName, attributes, parentTag, parentIsTagHelper);
        }

        public override IEnumerable<BoundAttributeDescriptor> GetBoundTagHelperAttributes(
            TagHelperDocumentContext documentContext,
            string attributeName,
            TagHelperBinding binding)
        {
            return _tagHelperFactsService.GetBoundTagHelperAttributes(documentContext, attributeName, binding);
        }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenTag(
            TagHelperDocumentContext documentContext,
            string tagName,
            string parentTag)
        {
            return _tagHelperFactsService.GetTagHelpersGivenTag(documentContext, tagName, parentTag);
        }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenParent(TagHelperDocumentContext documentContext, string parentTag)
        {
            return _tagHelperFactsService.GetTagHelpersGivenParent(documentContext, parentTag);
        }
    }
}
