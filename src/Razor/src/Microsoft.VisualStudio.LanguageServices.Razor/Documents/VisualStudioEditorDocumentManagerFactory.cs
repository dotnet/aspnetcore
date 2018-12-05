// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    [Shared]
    [ExportWorkspaceServiceFactory(typeof(EditorDocumentManager), ServiceLayer.Host)]
    internal class VisualStudioEditorDocumentManagerFactory : IWorkspaceServiceFactory
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public VisualStudioEditorDocumentManagerFactory(
            SVsServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactory,
            ForegroundDispatcher foregroundDispatcher)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (editorAdaptersFactory == null)
            {
                throw new ArgumentNullException(nameof(editorAdaptersFactory));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _serviceProvider = serviceProvider;
            _editorAdaptersFactory = editorAdaptersFactory;
            _foregroundDispatcher = foregroundDispatcher;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            var runningDocumentTable = (IVsRunningDocumentTable)_serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            var fileChangeTrackerFactory = workspaceServices.GetRequiredService<FileChangeTrackerFactory>();
            return new VisualStudioEditorDocumentManager(_foregroundDispatcher, fileChangeTrackerFactory, runningDocumentTable, _editorAdaptersFactory);
        }
    }
}
