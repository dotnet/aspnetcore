// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class EphemeralProjectSnapshot : ProjectSnapshot
    {
        private static readonly Task<IReadOnlyList<TagHelperDescriptor>> EmptyTagHelpers = Task.FromResult<IReadOnlyList<TagHelperDescriptor>>(Array.Empty<TagHelperDescriptor>());

        private readonly HostWorkspaceServices _services;
        private readonly Lazy<RazorProjectEngine> _projectEngine;

        public EphemeralProjectSnapshot(HostWorkspaceServices services, string filePath)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _services = services;
            FilePath = filePath;

            _projectEngine = new Lazy<RazorProjectEngine>(CreateProjectEngine);
        }

        public override RazorConfiguration Configuration => FallbackRazorConfiguration.MVC_2_1;

        public override IEnumerable<string> DocumentFilePaths => Array.Empty<string>();

        public override string FilePath { get; }

        public override bool IsInitialized => false;

        public override VersionStamp Version { get; } = VersionStamp.Default;

        public override Project WorkspaceProject => null;

        public override DocumentSnapshot GetDocument(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return null;
        }

        public override RazorProjectEngine GetProjectEngine()
        {
            return _projectEngine.Value;
        }

        public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync()
        {
            return EmptyTagHelpers;
        }

        public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> results)
        {
            results = EmptyTagHelpers.Result;
            return true;
        }

        private RazorProjectEngine CreateProjectEngine()
        {
            var factory = _services.GetRequiredService<ProjectSnapshotProjectEngineFactory>();
            return factory.Create(this);
        }
    }
}
