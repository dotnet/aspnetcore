// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Internal tracker for DefaultProjectSnapshot
    internal class ProjectState
    {
        private static readonly ImmutableDictionary<string, DocumentState> EmptyDocuments = ImmutableDictionary.Create<string, DocumentState>(FilePathComparer.Instance);
        private static readonly ImmutableDictionary<string, ImmutableArray<string>> EmptyImportsToRelatedDocuments = ImmutableDictionary.Create<string, ImmutableArray<string>>(FilePathComparer.Instance);
        private readonly object _lock;
        
        private ProjectEngineTracker _projectEngine;
        private ProjectTagHelperTracker _tagHelpers;

        public static ProjectState Create(HostWorkspaceServices services, HostProject hostProject, Project workspaceProject = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            return new ProjectState(services, hostProject, workspaceProject);
        }
        
        private ProjectState(
            HostWorkspaceServices services,
            HostProject hostProject,
            Project workspaceProject)
        {
            Services = services;
            HostProject = hostProject;
            WorkspaceProject = workspaceProject;
            Documents = EmptyDocuments;
            ImportsToRelatedDocuments = EmptyImportsToRelatedDocuments;
            Version = VersionStamp.Create();

            _lock = new object();
        }

        private ProjectState(
            ProjectState older,
            ProjectDifference difference,
            HostProject hostProject,
            Project workspaceProject,
            ImmutableDictionary<string, DocumentState> documents,
            ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments)
        {
            if (older == null)
            {
                throw new ArgumentNullException(nameof(older));
            }

            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            if (importsToRelatedDocuments == null)
            {
                throw new ArgumentNullException(nameof(importsToRelatedDocuments));
            }

            Services = older.Services;
            Version = older.Version.GetNewerVersion();

            HostProject = hostProject;
            WorkspaceProject = workspaceProject;
            Documents = documents;
            ImportsToRelatedDocuments = importsToRelatedDocuments;

            _lock = new object();

            _projectEngine = older._projectEngine?.ForkFor(this, difference);
            _tagHelpers = older._tagHelpers?.ForkFor(this, difference);
        }

        // Internal set for testing.
        public ImmutableDictionary<string, DocumentState> Documents { get; internal set; }

        // Internal set for testing.
        public ImmutableDictionary<string, ImmutableArray<string>> ImportsToRelatedDocuments { get; internal set; }

        public HostProject HostProject { get; }

        public HostWorkspaceServices Services { get; }

        public Project WorkspaceProject { get; }

        public VersionStamp Version { get; }

        // Computed State
        public ProjectEngineTracker ProjectEngine
        {
            get
            {
                if (_projectEngine == null)
                {
                    lock (_lock)
                    {
                        if (_projectEngine == null)
                        {
                            _projectEngine = new ProjectEngineTracker(this);
                        }
                    }
                }

                return _projectEngine;
            }
        }

        // Computed State
        public ProjectTagHelperTracker TagHelpers
        {
            get
            {
                if (_tagHelpers == null)
                {
                    lock (_lock)
                    {
                        if (_tagHelpers == null)
                        {
                            _tagHelpers = new ProjectTagHelperTracker(this);
                        }
                    }
                }

                return _tagHelpers;
            }
        }

        public ProjectState WithAddedHostDocument(HostDocument hostDocument, Func<Task<TextAndVersion>> loader)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            // Ignore attempts to 'add' a document with different data, we only
            // care about one, so it might as well be the one we have.
            if (Documents.ContainsKey(hostDocument.FilePath))
            {
                return this;
            }
            
            var documents = Documents.Add(hostDocument.FilePath, DocumentState.Create(Services, hostDocument, loader));

            // Compute the effect on the import map
            var importTargetPaths = ProjectEngine.GetImportDocumentTargetPaths(this, hostDocument.TargetPath);
            var importsToRelatedDocuments = AddToImportsToRelatedDocuments(ImportsToRelatedDocuments, hostDocument, importTargetPaths);
            
            // Now check if the updated document is an import - it's important this this happens after
            // updating the imports map.
            if (importsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
            {
                foreach (var relatedDocument in relatedDocuments)
                {
                    documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
                }
            }

            var state = new ProjectState(this, ProjectDifference.DocumentAdded, HostProject, WorkspaceProject, documents, importsToRelatedDocuments);
            return state;
        }

        public ProjectState WithRemovedHostDocument(HostDocument hostDocument)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            if (!Documents.ContainsKey(hostDocument.FilePath))
            {
                return this;
            }
            
            var documents = Documents.Remove(hostDocument.FilePath);

            // First check if the updated document is an import - it's important that this happens
            // before updating the imports map.
            if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
            {
                foreach (var relatedDocument in relatedDocuments)
                {
                    documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
                }
            }

            // Compute the effect on the import map
            var importTargetPaths = ProjectEngine.GetImportDocumentTargetPaths(this, hostDocument.TargetPath);
            var importsToRelatedDocuments = RemoveFromImportsToRelatedDocuments(ImportsToRelatedDocuments, hostDocument, importTargetPaths);

            var state = new ProjectState(this, ProjectDifference.DocumentRemoved, HostProject, WorkspaceProject, documents, importsToRelatedDocuments);
            return state;
        }

        public ProjectState WithChangedHostDocument(HostDocument hostDocument, SourceText sourceText, VersionStamp version)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
            {
                return this;
            }

            var documents = Documents.SetItem(hostDocument.FilePath, document.WithText(sourceText, version));

            if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
            {
                foreach (var relatedDocument in relatedDocuments)
                {
                    documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
                }
            }

            var state = new ProjectState(this, ProjectDifference.DocumentChanged, HostProject, WorkspaceProject, documents, ImportsToRelatedDocuments);
            return state;
        }

        public ProjectState WithChangedHostDocument(HostDocument hostDocument, Func<Task<TextAndVersion>> loader)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
            {
                return this;
            }

            var documents = Documents.SetItem(hostDocument.FilePath, document.WithTextLoader(loader));

            if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
            {
                foreach (var relatedDocument in relatedDocuments)
                {
                    documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
                }
            }

            var state = new ProjectState(this, ProjectDifference.DocumentChanged, HostProject, WorkspaceProject, documents, ImportsToRelatedDocuments);
            return state;
        }

        public ProjectState WithHostProject(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            if (HostProject.Configuration.Equals(hostProject.Configuration))
            {
                return this;
            }
            
            var documents = Documents.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.WithConfigurationChange(), FilePathComparer.Instance);

            // If the host project has changed then we need to recompute the imports map
            var importsToRelatedDocuments = EmptyImportsToRelatedDocuments;

            foreach (var document in documents)
            {
                var importTargetPaths = ProjectEngine.GetImportDocumentTargetPaths(this, document.Value.HostDocument.TargetPath);
                importsToRelatedDocuments = AddToImportsToRelatedDocuments(ImportsToRelatedDocuments, document.Value.HostDocument, importTargetPaths);
            }


            var state = new ProjectState(this, ProjectDifference.ConfigurationChanged, hostProject, WorkspaceProject, documents, importsToRelatedDocuments);
            return state;
        }

        public ProjectState WithWorkspaceProject(Project workspaceProject)
        {
            var difference = ProjectDifference.None;
            if (WorkspaceProject == null && workspaceProject != null)
            {
                difference |= ProjectDifference.WorkspaceProjectAdded;
            }
            else if (WorkspaceProject != null && workspaceProject == null)
            {
                difference |= ProjectDifference.WorkspaceProjectRemoved;
            }
            else
            {
                // We always update the snapshot right now when the project changes. This is how
                // we deal with changes to the content of C# sources.
                difference |= ProjectDifference.WorkspaceProjectChanged;
            }

            if (difference == ProjectDifference.None)
            {
                return this;
            }

            var documents = Documents.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.WithWorkspaceProjectChange(), FilePathComparer.Instance);
            var state = new ProjectState(this, difference, HostProject, workspaceProject, documents, ImportsToRelatedDocuments);
            return state;
        }

        private static ImmutableDictionary<string, ImmutableArray<string>> AddToImportsToRelatedDocuments(
            ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments,
            HostDocument hostDocument,
            List<string> importTargetPaths)
        {
            foreach (var importTargetPath in importTargetPaths)
            {
                if (!importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
                {
                    relatedDocuments = ImmutableArray.Create<string>();
                }

                relatedDocuments = relatedDocuments.Add(hostDocument.FilePath);
                importsToRelatedDocuments = importsToRelatedDocuments.SetItem(importTargetPath, relatedDocuments);
            }

            return importsToRelatedDocuments;
        }

        private static ImmutableDictionary<string, ImmutableArray<string>> RemoveFromImportsToRelatedDocuments(
            ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments,
            HostDocument hostDocument,
            List<string> importTargetPaths)
        {
            foreach (var importTargetPath in importTargetPaths)
            {
                if (importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
                {
                    relatedDocuments = relatedDocuments.Remove(hostDocument.FilePath);
                    if (relatedDocuments.Length > 0)
                    {
                        importsToRelatedDocuments = importsToRelatedDocuments.SetItem(importTargetPath, relatedDocuments);
                    }
                    else
                    {
                        importsToRelatedDocuments = importsToRelatedDocuments.Remove(importTargetPath);
                    }
                }
            }

            return importsToRelatedDocuments;
        }
    }
}
