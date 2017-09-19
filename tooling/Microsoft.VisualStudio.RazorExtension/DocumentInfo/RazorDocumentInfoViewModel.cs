// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Editor.Razor;

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

        public string Configuration => _documentTracker.Configuration?.DisplayName;

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

        public ProjectId ProjectId => _documentTracker.Project?.Id;

        public Workspace Workspace => _documentTracker.Workspace;
    }
}

#endif