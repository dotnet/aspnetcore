// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Routing
{
    public class OnNavigateArgs
    {
        public OnNavigateArgs(string path, CancellationTokenSource cancellationTokenSource)
        {
            Path = path;
            CancellationTokenSource = cancellationTokenSource;
        }

        public string Path { get; }

        public CancellationTokenSource CancellationTokenSource { get; }
    }
}
