// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.Editor
{
    internal class DefaultEditorSettingsManager : EditorSettingsManager
    {
        public override event EventHandler<EditorSettingsChangedEventArgs> Changed;

        private readonly object SettingsAccessorLock = new object();
        private EditorSettings _settings;

        public DefaultEditorSettingsManager()
        {
            _settings = EditorSettings.Default;
        }

        public override EditorSettings Current
        {
            get
            {
                lock (SettingsAccessorLock)
                {
                    return _settings;
                }
            }
        }

        public override void Update(EditorSettings updatedSettings)
        {
            if (updatedSettings == null)
            {
                throw new ArgumentNullException(nameof(updatedSettings));
            }

            lock (SettingsAccessorLock)
            {
                if (!_settings.Equals(updatedSettings))
                {
                    _settings = updatedSettings;
                    OnChanged();
                }
            }
        }

        private void OnChanged()
        {
            var args = new EditorSettingsChangedEventArgs(Current);
            Changed?.Invoke(this, args);
        }
    }
}
