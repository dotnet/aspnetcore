// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Watcher.Core
{
    public interface IProject
    {
        string ProjectFile { get; }

        IEnumerable<string> Files { get; }

        IEnumerable<string> ProjectDependencies { get; }
    }
}
