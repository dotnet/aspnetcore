// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Temporary code until we get access to these APIs
#if WORKSPACE_PROJECT_CONTEXT_FACTORY

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using IWorkspaceProjectContextFactory = Microsoft.VisualStudio.LanguageServices.ProjectSystem.IWorkspaceProjectContextFactory2;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    [Export(typeof(IWorkspaceProjectContextFactory))]
    internal class DefaultWorkspaceProjectContextFactory : IWorkspaceProjectContextFactory
    {
        public IWorkspaceProjectContext CreateProjectContext(string languageName, string projectDisplayName, string projectFilePath, Guid projectGuid, object hierarchy, string binOutputPath)
        {
            return new WorkspaceProjectContext();
        }

        public IWorkspaceProjectContext CreateProjectContext(string languageName, string projectDisplayName, string projectFilePath, Guid projectGuid, object hierarchy, string binOutputPath, ProjectExternalErrorReporter errorReporter)
        {
            return new WorkspaceProjectContext();
        }

        private class WorkspaceProjectContext : IWorkspaceProjectContext
        {
            public string DisplayName { get; set; }
            public string ProjectFilePath { get; set; }
            public Guid Guid { get; set; }
            public bool LastDesignTimeBuildSucceeded { get; set; }
            public string BinOutputPath { get; set; }

            public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
            {
            }

            public void AddAnalyzerReference(string referencePath)
            {
            }

            public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
            {
            }

            public void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties)
            {
            }

            public void AddSourceFile(string filePath, bool isInCurrentContext, IEnumerable<string> folderNames, SourceCodeKind sourceCodeKind)
            {
            }

            public void AddDynamicSourceFile(string filePath, IEnumerable<string> folderNames = null)
            {
            }

            public void Dispose()
            {
            }

            public void RemoveAdditionalFile(string filePath)
            {
            }

            public void RemoveAnalyzerReference(string referencePath)
            {
            }

            public void RemoveMetadataReference(string referencePath)
            {
            }

            public void RemoveProjectReference(IWorkspaceProjectContext project)
            {
            }

            public void RemoveSourceFile(string filePath)
            {
            }

            public void RemoveDynamicSourceFile(string filePath)
            {

            }

            public void SetOptions(string commandLineForOptions)
            {
            }

            public void SetRuleSetFile(string filePath)
            {
            }
        }
    }
}

#endif
