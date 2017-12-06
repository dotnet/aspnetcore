// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;

namespace Microsoft.Blazor.Browser
{
    public class BlazorBrowserFileProvider : IFileProvider
    {
        private EmbeddedFileProvider _embeddedFiles = new EmbeddedFileProvider(
            typeof(BlazorBrowserFileProvider).Assembly,
            "blazor");

        public IFileInfo GetFileInfo(string subpath)
            =>_embeddedFiles.GetFileInfo(subpath.Replace('/', '$'));

        public IDirectoryContents GetDirectoryContents(string subpath)
            => throw new NotImplementedException(); // Don't need to support this

        public IChangeToken Watch(string filter)
            => throw new NotImplementedException(); // Don't need to support this
    }
}
