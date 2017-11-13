// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(EditorSettingsManager))]
    internal class DefaultEditorSettingsManager : EditorSettingsManager
    {
        private readonly EditorSettingsManagerInternal _editorSettingsManager;

        [ImportingConstructor]
        public DefaultEditorSettingsManager(VisualStudioWorkspaceAccessor workspaceAccessor)
        {
            var razorLanguageServices = workspaceAccessor.Workspace.Services.GetLanguageServices(RazorLanguage.Name);
            _editorSettingsManager = razorLanguageServices.GetRequiredService<EditorSettingsManagerInternal>();
        }

        public override event EventHandler<EditorSettingsChangedEventArgs> Changed
        {
            add => _editorSettingsManager.Changed += value;
            remove => _editorSettingsManager.Changed -= value;
        }

        public override EditorSettings Current => _editorSettingsManager.Current;

        public override void Update(EditorSettings updateSettings)
        {
            _editorSettingsManager.Update(updateSettings);
        }
    }
}
