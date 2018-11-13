// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Watcher.Internal;

namespace Microsoft.DotNet.Watcher
{
    public class ProcessSpec
    {
        public string Executable { get; set; }
        public string WorkingDirectory { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();
        public IEnumerable<string> Arguments { get; set; }
        public OutputCapture OutputCapture { get; set; }

        public string ShortDisplayName()
            => Path.GetFileNameWithoutExtension(Executable);

        public bool IsOutputCaptured => OutputCapture != null;
    }
}
