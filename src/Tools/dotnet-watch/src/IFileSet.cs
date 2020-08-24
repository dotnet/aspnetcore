// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Watcher
{
    public interface IFileSet : IEnumerable<string>
    {
        bool IsNetCoreApp31OrNewer { get; }

        bool Contains(string filePath);
    }
}
