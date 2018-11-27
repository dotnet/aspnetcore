// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultProjectPathProvider : ProjectPathProvider
    {
        private readonly TextBufferProjectService _projectService;

        public DefaultProjectPathProvider(TextBufferProjectService projectService)
        {
            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _projectService = projectService;
        }

        public override bool TryGetProjectPath(ITextBuffer textBuffer, out string filePath)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            var project = _projectService.GetHostProject(textBuffer);
            if (project == null)
            {
                filePath = null;
                return false;
            }

            filePath = _projectService.GetProjectPath(project);
            return true;
        }
    }
}
