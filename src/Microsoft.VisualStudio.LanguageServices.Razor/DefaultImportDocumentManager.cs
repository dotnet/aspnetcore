// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [System.Composition.Shared]
    [Export(typeof(ImportDocumentManager))]
    internal class DefaultImportDocumentManager : ImportDocumentManager
    {
        private const uint FileChangeFlags = (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Add);

        private readonly IVsFileChangeEx _fileChangeService;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ErrorReporter _errorReporter;
        private readonly RazorTemplateEngineFactoryService _templateEngineFactoryService;
        private readonly Dictionary<string, ImportTracker> _importTrackerCache;

        public override event EventHandler<ImportChangedEventArgs> Changed;

        [ImportingConstructor]
        public DefaultImportDocumentManager(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            VisualStudioWorkspaceAccessor workspaceAccessor)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (workspaceAccessor == null)
            {
                throw new ArgumentNullException(nameof(workspaceAccessor));
            }

            _fileChangeService = serviceProvider.GetService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;

            var workspace = workspaceAccessor.Workspace;
            _foregroundDispatcher = workspace.Services.GetRequiredService<ForegroundDispatcher>();
            _errorReporter = workspace.Services.GetRequiredService<ErrorReporter>();

            var razorLanguageServices = workspace.Services.GetLanguageServices(RazorLanguage.Name);
            _templateEngineFactoryService = razorLanguageServices.GetRequiredService<RazorTemplateEngineFactoryService>();

            _importTrackerCache = new Dictionary<string, ImportTracker>(StringComparer.OrdinalIgnoreCase);
        }

        // This is only used for testing.
        internal DefaultImportDocumentManager(
            IVsFileChangeEx fileChangeService,
            RazorTemplateEngineFactoryService templateEngineFactoryService,
            ForegroundDispatcher foregroundDispatcher,
            ErrorReporter errorReporter)
        {
            _fileChangeService = fileChangeService;
            _templateEngineFactoryService = templateEngineFactoryService;
            _foregroundDispatcher = foregroundDispatcher;
            _errorReporter = errorReporter;

            _importTrackerCache = new Dictionary<string, ImportTracker>(StringComparer.OrdinalIgnoreCase);
        }

        public override void OnSubscribed(VisualStudioDocumentTracker tracker)
        {
            if (tracker == null)
            {
                throw new ArgumentNullException(nameof(tracker));
            }

            _foregroundDispatcher.AssertForegroundThread();

            var imports = GetImportItems(tracker);
            foreach (var import in imports)
            {
                var importFilePath = import.PhysicalPath;
                Debug.Assert(importFilePath != null);

                if (!_importTrackerCache.TryGetValue(importFilePath, out var importTracker))
                {
                    // First time seeing this import. Start tracking it.
                    importTracker = new ImportTracker(importFilePath);
                    _importTrackerCache[importFilePath] = importTracker;

                    StartListeningForChanges(importTracker);
                }

                importTracker.AssociatedDocuments.Add(tracker.FilePath);
            }
        }

        public override void OnUnsubscribed(VisualStudioDocumentTracker tracker)
        {
            if (tracker == null)
            {
                throw new ArgumentNullException(nameof(tracker));
            }

            _foregroundDispatcher.AssertForegroundThread();

            var imports = GetImportItems(tracker);
            foreach (var import in imports)
            {
                var importFilePath = import.PhysicalPath;
                Debug.Assert(importFilePath != null);

                if (_importTrackerCache.TryGetValue(importFilePath, out var importTracker))
                {
                    importTracker.AssociatedDocuments.Remove(tracker.FilePath);

                    if (importTracker.AssociatedDocuments.Count == 0)
                    {
                        // There are no open documents that care about this import. We no longer need to track it.
                        StopListeningForChanges(importTracker);
                        _importTrackerCache.Remove(importFilePath);
                    }
                }
            }
        }

        private IEnumerable<RazorProjectItem> GetImportItems(VisualStudioDocumentTracker tracker)
        {
            var projectDirectory = Path.GetDirectoryName(tracker.ProjectPath);
            var templateEngine = _templateEngineFactoryService.Create(projectDirectory, _ => { });
            var imports = templateEngine.GetImportItems(tracker.FilePath);

            return imports;
        }

        private void FireImportChanged(string importPath, ImportChangeKind kind)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var handler = Changed;
            if (handler != null && _importTrackerCache.TryGetValue(importPath, out var importTracker))
            {
                var args = new ImportChangedEventArgs(importPath, kind, importTracker.AssociatedDocuments);
                handler(this, args);
            }
        }

        // internal for testing.
        internal void OnFilesChanged(uint fileCount, string[] filePaths, uint[] fileChangeFlags)
        {
            for (var i = 0; i < fileCount; i++)
            {
                var kind = ImportChangeKind.Changed;
                var flag = (_VSFILECHANGEFLAGS)fileChangeFlags[i];

                if ((flag & _VSFILECHANGEFLAGS.VSFILECHG_Del) == _VSFILECHANGEFLAGS.VSFILECHG_Del)
                {
                    kind = ImportChangeKind.Removed;
                }
                else if ((flag & _VSFILECHANGEFLAGS.VSFILECHG_Add) == _VSFILECHANGEFLAGS.VSFILECHG_Add)
                {
                    kind = ImportChangeKind.Added;
                }

                FireImportChanged(filePaths[i], kind);
            }
        }

        private void StartListeningForChanges(ImportTracker importTracker)
        {
            try
            {
                if (importTracker.FileChangeCookie == VSConstants.VSCOOKIE_NIL)
                {
                    var hr = _fileChangeService.AdviseFileChange(
                            importTracker.FilePath,
                            FileChangeFlags,
                            new ImportDocumentEventSink(this, _foregroundDispatcher),
                            out var cookie);

                    Marshal.ThrowExceptionForHR(hr);

                    importTracker.FileChangeCookie = cookie;
                }
            }
            catch (Exception exception)
            {
                _errorReporter.ReportError(exception);
            }
        }

        private void StopListeningForChanges(ImportTracker importTracker)
        {
            try
            {
                if (importTracker.FileChangeCookie != VSConstants.VSCOOKIE_NIL)
                {
                    var hr = _fileChangeService.UnadviseFileChange(importTracker.FileChangeCookie);
                    Marshal.ThrowExceptionForHR(hr);
                    importTracker.FileChangeCookie = VSConstants.VSCOOKIE_NIL;
                }
            }
            catch (Exception exception)
            {
                _errorReporter.ReportError(exception);
            }
        }

        private class ImportTracker
        {
            public ImportTracker(string filePath)
            {
                FilePath = filePath;
                AssociatedDocuments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                FileChangeCookie = VSConstants.VSCOOKIE_NIL;
            }

            public string FilePath { get; }

            public HashSet<string> AssociatedDocuments { get; }

            public uint FileChangeCookie { get; set; }
        }

        private class ImportDocumentEventSink : IVsFileChangeEvents
        {
            private readonly DefaultImportDocumentManager _importDocumentManager;
            private readonly ForegroundDispatcher _foregroundDispatcher;

            public ImportDocumentEventSink(DefaultImportDocumentManager importDocumentManager, ForegroundDispatcher foregroundDispatcher)
            {
                _importDocumentManager = importDocumentManager;
                _foregroundDispatcher = foregroundDispatcher;
            }

            public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
            {
                _foregroundDispatcher.AssertForegroundThread();

                _importDocumentManager.OnFilesChanged(cChanges, rgpszFile, rggrfChange);

                return VSConstants.S_OK;
            }

            public int DirectoryChanged(string pszDirectory)
            {
                return VSConstants.S_OK;
            }
        }
    }
}
