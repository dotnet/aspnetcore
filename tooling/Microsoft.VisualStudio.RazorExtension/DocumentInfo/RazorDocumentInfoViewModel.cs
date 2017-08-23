// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.LanguageServices.Razor.Editor;

namespace Microsoft.VisualStudio.RazorExtension.DocumentInfo
{
    public class RazorDocumentInfoViewModel : NotifyPropertyChanged
    {
        private readonly VisualStudioDocumentTracker _documentTracker;

        public RazorDocumentInfoViewModel(VisualStudioDocumentTracker documentTracker)
        {
            if (documentTracker == null)
            {
                throw new ArgumentNullException(nameof(documentTracker));
            }

            _documentTracker = documentTracker;
        }

        public bool IsSupportedDocument => _documentTracker.IsSupportedProject;

        public Project Project
        {
            get
            {
                if (Workspace != null && ProjectId != null)
                {
                    return Workspace.CurrentSolution.GetProject(ProjectId);
                }

                return null;
            }
        }

        public ProjectId ProjectId => _documentTracker.ProjectId;

        public Workspace Workspace => _documentTracker.Workspace;

        public HostLanguageServices RazorLanguageServices => Workspace?.Services.GetLanguageServices(RazorLanguage.Name);

        public TagHelperResolver TagHelperResolver => RazorLanguageServices?.GetRequiredService<TagHelperResolver>();

        public RazorSyntaxFactsService RazorSyntaxFactsService => RazorLanguageServices?.GetRequiredService<RazorSyntaxFactsService>();

        public RazorTemplateEngineFactoryService RazorTemplateEngineFactoryService => RazorLanguageServices?.GetRequiredService<RazorTemplateEngineFactoryService>();

        public TagHelperCompletionService TagHelperCompletionService => RazorLanguageServices?.GetRequiredService<TagHelperCompletionService>();

        public TagHelperFactsService TagHelperFactsService => RazorLanguageServices?.GetRequiredService<TagHelperFactsService>();

    }
}

#endif