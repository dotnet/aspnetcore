// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DefaultImportDocumentSnapshot : DocumentSnapshot
    {
        private ProjectSnapshot _project;
        private RazorProjectItem _importItem;
        private SourceText _sourceText;
        private VersionStamp _version;

        public DefaultImportDocumentSnapshot(ProjectSnapshot project, RazorProjectItem item)
        {
            _project = project;
            _importItem = item;
            _version = VersionStamp.Default;
        }

        public override string FilePath => null;

        public override string TargetPath => null;

        public override bool SupportsOutput => false;

        public override ProjectSnapshot Project => _project;

        public override Task<RazorCodeDocument> GetGeneratedOutputAsync()
        {
            throw new NotSupportedException();
        }

        public override Task<VersionStamp> GetGeneratedOutputVersionAsync()
        {
            throw new NotSupportedException();
        }

        public override IReadOnlyList<DocumentSnapshot> GetImports()
        {
            return Array.Empty<DocumentSnapshot>();
        }

        public async override Task<SourceText> GetTextAsync()
        {
            using (var stream = _importItem.Read())
            using (var reader = new StreamReader(stream))
            {
                var content = await reader.ReadToEndAsync();
                _sourceText = SourceText.From(content);
            }

            return _sourceText;
        }

        public override Task<VersionStamp> GetTextVersionAsync()
        {
            return Task.FromResult(_version);
        }

        public override bool TryGetText(out SourceText result)
        {
            if (_sourceText != null)
            {
                result = _sourceText;
                return true;
            }

            result = null;
            return false;
        }

        public override bool TryGetTextVersion(out VersionStamp result)
        {
            result = _version;
            return true;
        }

        public override bool TryGetGeneratedOutput(out RazorCodeDocument result)
        {
            throw new NotSupportedException();
        }

        public override bool TryGetGeneratedOutputVersionAsync(out VersionStamp result)
        {
            throw new NotSupportedException();
        }
    }
}
