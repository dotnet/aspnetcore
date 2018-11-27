// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class EmptyTextLoader : TextLoader
    {
        private readonly string _filePath;
        private readonly VersionStamp _version;

        public EmptyTextLoader(string filePath)
        {
            _filePath = filePath;
            _version = VersionStamp.Create(); // Version will never change so this can be reused.
        }

        public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(TextAndVersion.Create(SourceText.From(""), _version, _filePath));
        }
    }
}