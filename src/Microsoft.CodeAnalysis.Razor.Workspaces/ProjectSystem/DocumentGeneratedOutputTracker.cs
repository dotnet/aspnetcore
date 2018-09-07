// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DocumentGeneratedOutputTracker
    {
        private readonly object _lock;

        private DocumentGeneratedOutputTracker _older;
        private Task<RazorCodeDocument> _task;
        
        private IReadOnlyList<TagHelperDescriptor> _tagHelpers;
        private IReadOnlyList<ImportItem> _imports;

        public DocumentGeneratedOutputTracker(DocumentGeneratedOutputTracker older)
        {
            _older = older;

            _lock = new object();
        }

        public bool IsResultAvailable => _task?.IsCompleted == true;

        public DocumentGeneratedOutputTracker Older => _older;

        public Task<RazorCodeDocument> GetGeneratedOutputInitializationTask(ProjectSnapshot project, DocumentSnapshot document)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (_task == null)
            {
                lock (_lock)
                {
                    if (_task == null)
                    {
                        _task = GetGeneratedOutputInitializationTaskCore(project, document);
                    }
                }
            }

            return _task;
        }

        public DocumentGeneratedOutputTracker Fork()
        {
            return new DocumentGeneratedOutputTracker(this);
        }

        private async Task<RazorCodeDocument> GetGeneratedOutputInitializationTaskCore(ProjectSnapshot project, DocumentSnapshot document)
        {
            var tagHelpers = await project.GetTagHelpersAsync().ConfigureAwait(false);
            var imports = await GetImportsAsync(project, document);

            if (_older != null && _older.IsResultAvailable)
            {
                var tagHelperDifference = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);
                tagHelperDifference.UnionWith(_older._tagHelpers);
                tagHelperDifference.SymmetricExceptWith(tagHelpers);

                var importDifference = new HashSet<ImportItem>();
                importDifference.UnionWith(_older._imports);
                importDifference.SymmetricExceptWith(imports);

                if (tagHelperDifference.Count == 0 && importDifference.Count == 0)
                {
                    // We can use the cached result.
                    var result = _older._task.Result;

                    // Drop reference so it can be GC'ed
                    _older = null;

                    // Cache the tag helpers and imports so the next version can use them
                    _tagHelpers = tagHelpers;
                    _imports = imports;

                    return result;
                }
            }

            // Drop reference so it can be GC'ed
            _older = null;

            // Cache the tag helpers and imports so the next version can use them
            _tagHelpers = tagHelpers;
            _imports = imports;

            var importSources = new List<RazorSourceDocument>();
            foreach (var item in imports)
            {
                var sourceDocument = await GetRazorSourceDocumentAsync(item.Import);
                importSources.Add(sourceDocument);
            }

            var documentSource = await GetRazorSourceDocumentAsync(document);

            var projectEngine = project.GetProjectEngine();

            var codeDocument = projectEngine.ProcessDesignTime(documentSource, importSources, tagHelpers);
            var csharpDocument = codeDocument.GetCSharpDocument();
            if (document is DefaultDocumentSnapshot defaultDocument)
            {
                defaultDocument.State.HostDocument.GeneratedCodeContainer.SetOutput(csharpDocument, defaultDocument);
            }

            return codeDocument;
        }

        private async Task<RazorSourceDocument> GetRazorSourceDocumentAsync(DocumentSnapshot document)
        {
            var sourceText = await document.GetTextAsync();

            return sourceText.GetRazorSourceDocument(document.FilePath);
        }

        private async Task<IReadOnlyList<ImportItem>> GetImportsAsync(ProjectSnapshot project, DocumentSnapshot document)
        {
            var imports = new List<ImportItem>();
            foreach (var snapshot in document.GetImports())
            {
                var versionStamp = await snapshot.GetTextVersionAsync();
                imports.Add(new ImportItem(snapshot.FilePath, versionStamp, snapshot));
            }

            return imports;
        }

        private struct ImportItem : IEquatable<ImportItem>
        {
            public ImportItem(string filePath, VersionStamp versionStamp, DocumentSnapshot import)
            {
                FilePath = filePath;
                VersionStamp = versionStamp;
                Import = import;
            }

            public string FilePath { get; }

            public VersionStamp VersionStamp { get; }

            public DocumentSnapshot Import { get; }

            public bool Equals(ImportItem other)
            {
                return
                    FilePathComparer.Instance.Equals(FilePath, other.FilePath) &&
                    VersionStamp == other.VersionStamp;
            }

            public override bool Equals(object obj)
            {
                return obj is ImportItem item ? Equals(item) : false;
            }

            public override int GetHashCode()
            {
                var hash = new HashCodeCombiner();
                hash.Add(FilePath, FilePathComparer.Instance);
                hash.Add(VersionStamp);
                return hash;
            }
        }
    }
}
