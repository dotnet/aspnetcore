// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Watcher.Internal
{
    internal class OutputSink
    {
        public OutputCapture Current { get; private set; }
        public OutputCapture StartCapture()
        {
            return (Current = new OutputCapture());
        }

        public class OutputCapture
        {
            private readonly List<string> _lines = new List<string>();
            public IEnumerable<string> Lines => _lines;
            public void WriteOutputLine(string line) => _lines.Add(line);
            public void WriteErrorLine(string line) => _lines.Add(line);
            public string GetAllLines(string prefix) => string.Join(Environment.NewLine, _lines.Select(l => prefix + l));
        }
    }
}