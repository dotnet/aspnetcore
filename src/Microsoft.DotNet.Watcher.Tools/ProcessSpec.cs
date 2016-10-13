// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Watcher
{
    public class ProcessSpec
    {
        public string Executable { get; set; }
        public string WorkingDirectory { get; set; }
        public IEnumerable<string> Arguments { get; set; }

        public string ShortDisplayName() 
            => Path.GetFileNameWithoutExtension(Executable);
    }
}
