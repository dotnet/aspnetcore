// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    public abstract class VisualStudioDocumentTracker
    {
        public abstract event EventHandler ContextChanged;

        public abstract bool IsSupportedProject { get; }

        public abstract ProjectId ProjectId { get; }

        public abstract Workspace Workspace { get; }

        public abstract ITextBuffer TextBuffer { get; }

        public abstract IReadOnlyList<ITextView> TextViews { get; }
    }
}
