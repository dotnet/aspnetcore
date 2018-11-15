// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.DotNet.Watcher.Internal
{
    public class OutputSink
    {
        public OutputCapture Current { get; private set; }
        public OutputCapture StartCapture()
        {
            return (Current = new OutputCapture());
        }
    }
}