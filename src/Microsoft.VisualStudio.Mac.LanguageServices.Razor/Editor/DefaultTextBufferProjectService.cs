// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.Editor
{
    /// <summary>
    /// Infrastructure methods to find project information from an <see cref="ITextBuffer"/>.
    /// </summary>
    internal class DefaultTextBufferProjectService : TextBufferProjectService
    {
        private readonly ITextDocumentFactoryService _documentFactory;
        private readonly ErrorReporter _errorReporter;

        public DefaultTextBufferProjectService(
            ITextDocumentFactoryService documentFactory,
            ErrorReporter errorReporter)
        {
            if (documentFactory == null)
            {
                throw new ArgumentNullException(nameof(documentFactory));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            _documentFactory = documentFactory;
            _errorReporter = errorReporter;
        }

        public override object GetHostProject(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            // If there's no document we can't find the FileName, or look for an associated project.
            if (!_documentFactory.TryGetTextDocument(textBuffer, out var textDocument))
            {
                return null;
            }

            var projectsContainingFilePath = IdeApp.Workspace.GetProjectsContainingFile(textDocument.FilePath);
            foreach (var project in projectsContainingFilePath)
            {
                if (!(project is DotNetProject))
                {
                    continue;
                }

                var projectFile = project.GetProjectFile(textDocument.FilePath);
                if (!projectFile.IsHidden)
                {
                    return project;
                }
            }

            return null;
        }

        public override string GetProjectPath(object project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var dotnetProject = (DotNetProject)project;
            return dotnetProject.FileName.FullPath;
        }

        // VisualStudio for Mac only supports ASP.NET Core Razor.
        public override bool IsSupportedProject(object project) => true;

        public override string GetProjectName(object project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var dotnetProject = (DotNetProject)project;

            return dotnetProject.Name;
        }
    }
}
