// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DocumentImportsTracker
    {
        private readonly object _lock;

        private IReadOnlyList<DocumentSnapshot> _imports;

        public DocumentImportsTracker()
        {
            _lock = new object();
        }

        public IReadOnlyList<DocumentSnapshot> GetImports(ProjectSnapshot project, DocumentSnapshot document)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (_imports == null)
            {
                lock (_lock)
                {
                    if (_imports == null)
                    {
                        _imports = GetImportsCore(project, document);
                    }
                }
            }

            return _imports;
        }

        private IReadOnlyList<DocumentSnapshot> GetImportsCore(ProjectSnapshot project, DocumentSnapshot document)
        {
            var projectEngine = project.GetProjectEngine();
            var importFeature = projectEngine.ProjectFeatures.OfType<IImportProjectFeature>().FirstOrDefault();
            var projectItem = projectEngine.FileSystem.GetItem(document.FilePath);
            var importItems = importFeature?.GetImports(projectItem).Where(i => i.Exists);
            if (importItems == null)
            {
                return Array.Empty<DocumentSnapshot>();
            }

            var imports = new List<DocumentSnapshot>();
            foreach (var item in importItems)
            {
                if (item.PhysicalPath == null)
                {
                    // This is a default import.
                    var defaultImport = new DefaultImportDocumentSnapshot(project, item);
                    imports.Add(defaultImport);
                }
                else
                {
                    var import = project.GetDocument(item.PhysicalPath);
                    if (import == null)
                    {
                        // We are not tracking this document in this project. So do nothing.
                        continue;
                    }

                    imports.Add(import);
                }
            }

            return imports;
        }

        private class DefaultImportDocumentSnapshot : DocumentSnapshot
        {
            private ProjectSnapshot _project;
            private RazorProjectItem _importItem;
            private SourceText _sourceText;
            private VersionStamp _version;
            private DocumentGeneratedOutputTracker _generatedOutput;

            public DefaultImportDocumentSnapshot(ProjectSnapshot project, RazorProjectItem item)
            {
                _project = project;
                _importItem = item;
                _version = VersionStamp.Default;
                _generatedOutput = new DocumentGeneratedOutputTracker(null);
            }

            public override string FilePath => null;

            public override string TargetPath => null;

            public override ProjectSnapshot Project => _project;

            public override Task<RazorCodeDocument> GetGeneratedOutputAsync()
            {
                return _generatedOutput.GetGeneratedOutputInitializationTask(_project, this);
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
                if (_generatedOutput.IsResultAvailable)
                {
                    result = GetGeneratedOutputAsync().Result;
                    return true;
                }

                result = null;
                return false;
            }
        }
    }
}
