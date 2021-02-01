// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Internal;

namespace Microsoft.DotNet.Watcher
{
    public interface IFileSetFactory
    {
        Task<FileSet> CreateAsync(CancellationToken cancellationToken);
    }
}
