// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.ProjectSystem
{
    // This is a marker class to allow us to know when potential breaking changes might impact live share.
    // This is intentionally not abstract so any API changes that happen to ProjectSnapshot will break this.
    internal class LiveShareProjectSnapshotBase : ProjectSnapshot
    {
        public override RazorConfiguration Configuration => throw new NotImplementedException();

        public override IEnumerable<string> DocumentFilePaths => throw new NotImplementedException();

        public override string FilePath => throw new NotImplementedException();

        public override bool IsInitialized => throw new NotImplementedException();

        public override VersionStamp Version => throw new NotImplementedException();

        public override Project WorkspaceProject => throw new NotImplementedException();

        public override DocumentSnapshot GetDocument(string filePath) => throw new NotImplementedException();

        public override bool IsImportDocument(DocumentSnapshot document) => throw new NotImplementedException();

        public override IEnumerable<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document) => throw new NotImplementedException();

        public override RazorProjectEngine GetProjectEngine() => throw new NotImplementedException();

        public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync() => throw new NotImplementedException();

        public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> result) => throw new NotImplementedException();
    }
}
