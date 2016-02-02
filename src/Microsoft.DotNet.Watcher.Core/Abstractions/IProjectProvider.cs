// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.DotNet.Watcher.Core
{
    public interface IProjectProvider
    {
        bool TryReadProject(string projectFile, out IProject project, out string errors);
    }
}
