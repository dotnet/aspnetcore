// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectSnapshot
    {
        public abstract RazorConfiguration Configuration { get; }

        public abstract IEnumerable<string> DocumentFilePaths { get; }

        public abstract string FilePath { get; }

        public abstract bool IsInitialized { get; }

        public abstract VersionStamp Version { get; }

        public abstract Project WorkspaceProject { get; }

        public abstract RazorProjectEngine GetProjectEngine();

        public abstract DocumentSnapshot GetDocument(string filePath);

        public abstract Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync();

        public abstract bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> results);
    }
}