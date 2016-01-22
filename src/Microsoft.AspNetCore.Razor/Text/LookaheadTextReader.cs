// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Text
{
    public abstract class LookaheadTextReader : TextReader
    {
        public abstract SourceLocation CurrentLocation { get; }
        public abstract IDisposable BeginLookahead();
        public abstract void CancelBacktrack();
    }
}
