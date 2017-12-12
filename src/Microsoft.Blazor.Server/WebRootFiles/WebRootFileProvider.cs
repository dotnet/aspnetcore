// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;

namespace Microsoft.Blazor.Server.WebRootFiles
{
    internal static class WebRootFileProvider
    {
        public static IFileProvider Instantiate(
            string clientWebRoot, string assemblyName, IEnumerable<IFileInfo> binFiles)
            => new CompositeFileProvider(
                new IndexHtmlFileProvider(clientWebRoot, assemblyName, binFiles),
                new PhysicalFileProvider(clientWebRoot));
    }
}
