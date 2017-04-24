// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    public abstract class RazorDocumentTracker
    {
        public abstract event EventHandler ContextChanged;

        public abstract bool IsSupportedDocument { get; }

        public abstract ProjectId ProjectId { get; }

        public abstract SourceTextContainer TextContainer { get; }

        public abstract Workspace Workspace { get; }
    }
}
