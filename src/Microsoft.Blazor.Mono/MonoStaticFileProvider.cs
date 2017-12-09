// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;

namespace Microsoft.Blazor.Mono
{
    public class MonoStaticFileProvider : IFileProvider
    {
        private EmbeddedFileProvider _embeddedFiles = new EmbeddedFileProvider(
            typeof(MonoStaticFileProvider).Assembly,
            "mono");

        public static MonoStaticFileProvider Instance = new MonoStaticFileProvider();

        private MonoStaticFileProvider()
        {
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // EmbeddedFileProvider can't find resources whose names include '/' (or '\'),
            // so the resources in the assembly use '$' as a directory separator
            var possibleResourceName = subpath.Replace('/', '$');
            return _embeddedFiles.GetFileInfo(possibleResourceName);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
            => _embeddedFiles.GetDirectoryContents(subpath);

        public IChangeToken Watch(string filter)
            => throw new NotImplementedException(); // Don't need to support this
    }
}
