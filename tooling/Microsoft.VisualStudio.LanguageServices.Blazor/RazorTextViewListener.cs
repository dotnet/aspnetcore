// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Blazor
{
    [ContentType(RazorLanguage.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Export(typeof(IWpfTextViewConnectionListener))]
    internal class BlazorOpenDocumentTracker : IWpfTextViewConnectionListener
    {
        private readonly RazorEditorFactoryService _editorFactory;
        private readonly Workspace _workspace;

        private readonly HashSet<IWpfTextView> _openViews;

        private Type _codeGeneratorType;
        private Type _projectSnapshotManagerType;

        [ImportingConstructor]
        public BlazorOpenDocumentTracker(
            RazorEditorFactoryService editorFactory,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (editorFactory == null)
            {
                throw new ArgumentNullException(nameof(editorFactory));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _editorFactory = editorFactory;
            _workspace = workspace;

            _openViews = new HashSet<IWpfTextView>();

            _workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
        }

        public Workspace Workspace => _workspace;

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

            _openViews.Add(textView);
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

            _openViews.Remove(textView);
        }

        // We're watching the Roslyn workspace for changes specifically because we want
        // to know when the language service has processed a file change.
        //
        // It might be more elegant to use a file watcher rather than sniffing workspace events
        // but there would be a delay between the file watcher and Roslyn processing the update.
        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.DocumentChanged:
                case WorkspaceChangeKind.DocumentInfoChanged:
                case WorkspaceChangeKind.DocumentReloaded:
                case WorkspaceChangeKind.DocumentRemoved:

                    var document = e.NewSolution.GetDocument(e.DocumentId);
                    if (document == null || document.FilePath == null)
                    {
                        break;
                    }

                    if (!document.FilePath.EndsWith(".g.i.cs"))
                    {
                        break;
                    }

                    OnDeclarationsChanged(e.NewSolution.GetProject(e.ProjectId));
                    break;
            }
        }

        private void OnDeclarationsChanged(Project project)
        {
            // In 15.8 the Razor Language Services provides the actual Tag Helper discovery logic.
            // We can interface with that if we're running in a 15.8 build.
            if (_projectSnapshotManagerType == null && _codeGeneratorType == null)
            {
                try
                {
                    var assembly = typeof(Microsoft.CodeAnalysis.Razor.IProjectEngineFactory).Assembly;
                    _projectSnapshotManagerType = assembly.GetType("Microsoft.CodeAnalysis.Razor.ProjectSystem.ProjectSnapshotManager");
                }
                catch (Exception)
                {
                    // If the above fails, try the 15.7 logic.
                }
            }

            if (_projectSnapshotManagerType != null)
            {
                try
                {
                    var languageServices = _workspace.Services.GetLanguageServices(RazorLanguage.Name);
                    var manager = languageServices
                        .GetType()
                        .GetMethod(nameof(HostLanguageServices.GetService))
                        .MakeGenericMethod(_projectSnapshotManagerType)
                        .Invoke(languageServices, null);

                    manager.GetType().GetMethod("WorkspaceProjectChanged").Invoke(manager, new object[] { project, });
                    return;
                }
                catch (Exception)
                {
                    // If the above fails, try the 15.7 logic.
                }
            }


            // This is a design-time Razor file change.Go poke all of the open
            // Razor documents and tell them to update.
            var buffers = _openViews
                .SelectMany(v => v.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType("RazorCSharp")))
                .Distinct()
                .ToArray();

            if (_codeGeneratorType == null)
            {
                try
                {
                    var assembly = Assembly.Load("Microsoft.VisualStudio.Web.Editors.Razor.4_0, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                    _codeGeneratorType = assembly.GetType("Microsoft.VisualStudio.Web.Editors.Razor.RazorCodeGenerator");
                }
                catch (Exception)
                {
                    // If this fails, just unsubscribe. We won't be able to do our work, so just don't waste time.
                    _workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
                }
            }

            foreach (var buffer in buffers)
            {
                try
                {
                    var tryGetFromBuffer = _codeGeneratorType.GetMethod("TryGetFromBuffer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    var args = new object[] { buffer, null };
                    if (!(bool)tryGetFromBuffer.Invoke(null, args) || args[1] == null)
                    {
                        continue;
                    }

                    var field = _codeGeneratorType.GetField("_tagHelperDescriptorResolver", BindingFlags.Instance | BindingFlags.NonPublic);
                    var resolver = field.GetValue(args[1]);
                    if (resolver == null)
                    {
                        continue;
                    }

                    var reset = resolver.GetType().GetMethod("ResetTagHelperDescriptors", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (reset == null)
                    {
                        continue;
                    }

                    reset.Invoke(resolver, Array.Empty<object>());
                }
                catch (Exception)
                {
                    // If this fails, just unsubscribe. We won't be able to do our work, so just don't waste time.
                    _workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
                }
            }
        }
    }
}