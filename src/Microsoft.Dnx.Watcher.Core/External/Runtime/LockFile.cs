// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Dnx.Runtime
{
    public class LockFile
    {
        public int Version { get; set; }

        public IList<LockFileProjectLibrary> ProjectLibraries { get; set; } = new List<LockFileProjectLibrary>();
    }
}