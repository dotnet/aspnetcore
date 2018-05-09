// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DocumentGeneratedOutputTracker
    {
        private readonly object _lock;

        private DocumentGeneratedOutputTracker _older;
        private Task<RazorCodeDocument> _task;
        
        private IReadOnlyList<TagHelperDescriptor> _tagHelpers;

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
            if (_older != null && _older.IsResultAvailable)
            {
                var difference = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);
                difference.UnionWith(_older._tagHelpers);
                difference.SymmetricExceptWith(tagHelpers);

                if (difference.Count == 0)
                {
                    // We can use the cached result.
                    var result = _older._task.Result;

                    // Drop reference so it can be GC'ed
                    _older = null;

                    // Cache the tag helpers so the next version can use them
                    _tagHelpers = tagHelpers;

                    return result;
                }
            }

            // Drop reference so it can be GC'ed
            _older = null;


            // Cache the tag helpers so the next version can use them
            _tagHelpers = tagHelpers;

            var projectEngine = project.GetProjectEngine();
            var projectItem = projectEngine.FileSystem.GetItem(document.FilePath);
            return projectItem == null ? null : projectEngine.ProcessDesignTime(projectItem);
        }
    }
}
