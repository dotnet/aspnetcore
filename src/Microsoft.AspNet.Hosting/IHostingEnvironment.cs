// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileSystems;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    [AssemblyNeutral]
    public interface IHostingEnvironment
    {
        string EnvironmentName { get; set; }

        string WebRoot { get; }

        IFileSystem WebRootFileSystem { get; }
    }
}