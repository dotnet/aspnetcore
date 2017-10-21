// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.Editor
{
    public abstract class EditorSettingsManager : ILanguageService
    {
        public abstract event EventHandler<EditorSettingsChangedEventArgs> Changed;

        public abstract EditorSettings Current { get; }

        public abstract void Update(EditorSettings updateSettings);
    }
}
