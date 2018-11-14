// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Watcher.Internal
{
    public class OutputCapture
    {
        private readonly List<string> _lines = new List<string>();
        public IEnumerable<string> Lines => _lines;
        public void AddLine(string line) => _lines.Add(line);
    }
}