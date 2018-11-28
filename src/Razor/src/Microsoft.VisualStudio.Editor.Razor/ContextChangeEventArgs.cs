// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public sealed class ContextChangeEventArgs : EventArgs
    {
        public ContextChangeEventArgs(ContextChangeKind kind)
        {
            Kind = kind;
        }

        public ContextChangeKind Kind { get; }
    }
}