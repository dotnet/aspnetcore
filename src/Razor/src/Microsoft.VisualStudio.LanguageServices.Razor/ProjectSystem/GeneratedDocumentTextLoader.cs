// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class GeneratedDocumentTextLoader : TextLoader
    {
        private readonly DocumentSnapshot _document;
        private readonly string _filePath;
        private readonly VersionStamp _version;

        public GeneratedDocumentTextLoader(DocumentSnapshot document, string filePath)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _document = document;
            _filePath = filePath;
            _version = VersionStamp.Create();
        }

        public override async Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            var output = await _document.GetGeneratedOutputAsync().ConfigureAwait(false);
            return TextAndVersion.Create(SourceText.From(output.GetCSharpDocument().GeneratedCode), _version, _filePath);
        }
    }
}
