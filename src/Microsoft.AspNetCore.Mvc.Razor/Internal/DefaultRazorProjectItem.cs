// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DefaultRazorProjectItem : RazorProjectItem
    {
        private readonly IFileInfo _fileInfo;

        public DefaultRazorProjectItem(IFileInfo fileInfo, string basePath, string path)
        {
            _fileInfo = fileInfo;
            BasePath = basePath;
            Path = path;
        }

        public override string BasePath { get; }

        public override string Path { get; }

        public override string PhysicalPath => _fileInfo.PhysicalPath;

        public override Stream Read()
        {
            return _fileInfo.CreateReadStream();
        }
    }
}
