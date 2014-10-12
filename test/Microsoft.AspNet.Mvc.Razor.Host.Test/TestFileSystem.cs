// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNet.FileSystems;
using Moq;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class TestFileSystem : IFileSystem
    {
        private readonly Dictionary<string, IFileInfo> _lookup =
            new Dictionary<string, IFileInfo>(StringComparer.Ordinal);

        public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
        {
            throw new NotImplementedException();
        }

        public void AddFile(string path, string contents)
        {
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.CreateReadStream())
                    .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(contents)));
            fileInfo.SetupGet(f => f.PhysicalPath)
                    .Returns(path);
            fileInfo.SetupGet(f => f.Name)
                    .Returns(Path.GetFileName(path));
            AddFile(path, fileInfo.Object);
        }

        public void AddFile(string path, IFileInfo contents)
        {
            _lookup.Add(path, contents);
        }

        public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
        {
            return _lookup.TryGetValue(subpath, out fileInfo);
        }

        public bool TryGetParentPath(string subpath, out string parentPath)
        {
            parentPath = Path.GetDirectoryName(subpath);
            return !string.IsNullOrEmpty(parentPath);
        }
    }
}