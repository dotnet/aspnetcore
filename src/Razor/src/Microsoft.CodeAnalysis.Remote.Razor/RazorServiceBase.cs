// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal abstract class RazorServiceBase : ServiceHubServiceBase
    {
        public RazorServiceBase(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            RazorServices = new RazorServices();

            Rpc.JsonSerializer.Converters.RegisterRazorConverters();

            // Due to this issue - https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950
            // We need to manually start the RPC connection. Otherwise we'd be opting ourselves into 
            // race condition prone call paths.
            Rpc.StartListening();
        }

        protected RazorServices RazorServices { get; }

        protected virtual async Task<ProjectSnapshot> GetProjectSnapshotAsync(ProjectSnapshotHandle projectHandle, CancellationToken cancellationToken)
        {
            if (projectHandle == null)
            {
                throw new ArgumentNullException(nameof(projectHandle));
            }

            var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
            var workspaceProject = solution.GetProject(projectHandle.WorkspaceProjectId);

            return new SerializedProjectSnapshot(projectHandle.FilePath, projectHandle.Configuration, workspaceProject);
        }

        private class SerializedProjectSnapshot : ProjectSnapshot
        {
            public SerializedProjectSnapshot(string filePath, RazorConfiguration configuration, Project workspaceProject)
            {
                FilePath = filePath;
                Configuration = configuration;
                WorkspaceProject = workspaceProject;

                IsInitialized = true;
                Version = VersionStamp.Default;
            }

            public override RazorConfiguration Configuration { get; }

            public override IEnumerable<string> DocumentFilePaths => Array.Empty<string>();

            public override string FilePath { get; }

            public override bool IsInitialized { get; }

            public override VersionStamp Version { get; }

            public override Project WorkspaceProject { get; }

            public override DocumentSnapshot GetDocument(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                return null;
            }

            public override bool IsImportDocument(DocumentSnapshot document)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document)
            {
                throw new NotImplementedException();
            }

            public override RazorProjectEngine GetProjectEngine()
            {
                throw new NotImplementedException();
            }

            public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync()
            {
                throw new NotImplementedException();
            }

            public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> result)
            {
                throw new NotImplementedException();
            }
        }
    }
}
