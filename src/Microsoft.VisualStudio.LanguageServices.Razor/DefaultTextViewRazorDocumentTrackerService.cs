// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [ContentType(RazorLanguage.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Export(typeof(IWpfTextViewConnectionListener))]
    [Export(typeof(TextViewRazorDocumentTrackerService))]
    internal class DefaultTextViewRazorDocumentTrackerService : TextViewRazorDocumentTrackerService, IWpfTextViewConnectionListener
    {
        private readonly ITextDocumentFactoryService _documentFactory;
        private readonly IServiceProvider _services;
        private readonly Workspace _workspace;

        private RunningDocumentTable _runningDocumentTable;
        private IVsSolution _solution;

        [ImportingConstructor]
        public DefaultTextViewRazorDocumentTrackerService(
            [Import(typeof(SVsServiceProvider))] IServiceProvider services,
            ITextDocumentFactoryService documentFactory,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (documentFactory == null)
            {
                throw new ArgumentNullException(nameof(documentFactory));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _services = services;
            _documentFactory = documentFactory;
            _workspace = workspace;
        }

        // Lazy to avoid needing this in unit tests.
        private RunningDocumentTable RunningDocumentTable
        {
            get
            {
                if (_runningDocumentTable == null)
                {
                    _runningDocumentTable = new RunningDocumentTable(_services);
                }

                return _runningDocumentTable;
            }
        }

        // Lazy to avoid needing this in unit tests.
        private IVsSolution Solution
        {
            get
            {
                if (_solution == null)
                {
                    _solution = (IVsSolution)_services.GetService(typeof(SVsSolution));
                }

                return _solution;
            }
        }

        public Workspace Workspace => _workspace;

        public override RazorDocumentTracker CreateTracker(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (!textView.Properties.TryGetProperty<RazorDocumentTracker>(typeof(RazorDocumentTracker), out var tracker))
            {
                tracker = new DefaultTextViewRazorDocumentTracker(this, textView);
                textView.Properties.AddProperty(typeof(RazorDocumentTracker), tracker);
            }

            return tracker;
        }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            // This means a Razor buffer has been attached to this ITextView or the ITextView is just opening with Razor content.
            //
            // Call CreateTracker just for the side effect. The tracker will do all of the real work after it's initialized.
            CreateTracker(textView);
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (textView == null)
            {
                throw new ArgumentException(nameof(textView));
            }

            if (subjectBuffers == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffers));
            }

            // This means a Razor buffer has be detached from this ITextView or the ITextView is closing.
            //
            // Do nothing, the tracker will update itself.
        }

        public virtual IVsHierarchy GetProject(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            // If there's no document we can't find the FileName, or look for a matching hierarchy.
            if (!_documentFactory.TryGetTextDocument(textBuffer, out var textDocument))
            {
                return null;
            }

            RunningDocumentTable.FindDocument(textDocument.FilePath, out var hierarchy, out uint itemId, out uint cookie);

            // We don't currently try to look a Roslyn ProjectId at this point, we just want to know some
            // basic things.
            // See https://github.com/dotnet/roslyn/blob/4e3db2b7a0732d45a720e9ed00c00cd22ab67a14/src/VisualStudio/Core/SolutionExplorerShim/HierarchyItemToProjectIdMap.cs#L47
            // for a more complete implementation.
            return hierarchy;
        }

        public virtual bool IsSupportedProject(IVsHierarchy project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
            
            return project.IsCapabilityMatch("DotNetCoreWeb");
        }

        public static IEnumerable<ITextView> GetTextViews(ITextBuffer textBuffer)
        {
            // TODO: Extract text views from buffer

            return new[] { (ITextView)null };
        }
    }
}
