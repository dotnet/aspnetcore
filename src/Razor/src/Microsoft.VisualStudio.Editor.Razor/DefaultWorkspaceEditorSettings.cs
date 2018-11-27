// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultWorkspaceEditorSettings : WorkspaceEditorSettings
    {
        private readonly EditorSettingsManager _editorSettingsManager;
        private readonly EventHandler<EditorSettingsChangedEventArgs> _onChanged;
        private EventHandler<EditorSettingsChangedEventArgs> _changed;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private int _listenerCount = 0;

        public DefaultWorkspaceEditorSettings(ForegroundDispatcher foregroundDispatcher, EditorSettingsManager editorSettingsManager)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (editorSettingsManager == null)
            {
                throw new ArgumentNullException(nameof(editorSettingsManager));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _editorSettingsManager = editorSettingsManager;
            _onChanged = OnChanged;
        }

        public override event EventHandler<EditorSettingsChangedEventArgs> Changed
        {
            add
            {
                _foregroundDispatcher.AssertForegroundThread();

                _listenerCount++;
                _changed += value;

                if (_listenerCount == 1)
                {
                    // We bind to the editor settings manager only when we have listeners to avoid leaking memory.
                    // Basically we're relying on anyone listening to us to have an understanding of when they're going
                    // to be torn down. In Razor's case this will just be the document tracker factory (which does know).
                    AttachToEditorSettingsManager();
                }
            }
            remove
            {
                _foregroundDispatcher.AssertForegroundThread();

                _listenerCount--;
                _changed -= value;

                if (_listenerCount == 0)
                {
                    // We detatch from the editor settings manager when no one is listening to allow us to be garbage
                    // collected in the case that the workspace is tearing down.
                    DetachFromEditorSettingsManager();
                }
            }
        }

        // Internal for testing
        internal virtual void AttachToEditorSettingsManager()
        {
            _editorSettingsManager.Changed += _onChanged;
        }

        // Internal for testing
        internal virtual void DetachFromEditorSettingsManager()
        {
            _editorSettingsManager.Changed -= _onChanged;
        }

        public override EditorSettings Current => _editorSettingsManager.Current;

        // Internal for testing
        internal void OnChanged(object sender, EditorSettingsChangedEventArgs e)
        {
            _foregroundDispatcher.AssertForegroundThread();

            Debug.Assert(_changed != null, nameof(OnChanged) + " should not be invoked when there are no listeners.");

            var args = new EditorSettingsChangedEventArgs(Current);
            _changed?.Invoke(this, args);
        }
    }
}
