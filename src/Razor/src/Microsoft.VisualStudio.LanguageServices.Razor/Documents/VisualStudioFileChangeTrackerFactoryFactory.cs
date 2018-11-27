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
    [ExportWorkspaceServiceFactory(typeof(FileChangeTrackerFactory), ServiceLayer.Host)]
    internal class VisualStudioFileChangeTrackerFactoryFactory : IWorkspaceServiceFactory
    {
        private readonly IVsFileChangeEx _fileChangeService;
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public VisualStudioFileChangeTrackerFactoryFactory(ForegroundDispatcher foregroundDispatcher, SVsServiceProvider serviceProvider)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _fileChangeService = serviceProvider.GetService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
        }
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            var errorReporter = workspaceServices.GetRequiredService<ErrorReporter>();
            return new VisualStudioFileChangeTrackerFactory(_foregroundDispatcher, errorReporter, _fileChangeService);
        }
    }
}
