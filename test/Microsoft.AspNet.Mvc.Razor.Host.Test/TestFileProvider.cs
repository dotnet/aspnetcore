// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class TestFileProvider : IFileProvider
    {
        private readonly Dictionary<string, IFileInfo> _lookup =
            new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, TestFileTrigger> _fileTriggers =
            new Dictionary<string, TestFileTrigger>(StringComparer.Ordinal);

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotSupportedException();
        }

        public void AddFile(string path, string contents)
        {
            var fileInfo = new TestFileInfo
            {
                Content = contents,
                PhysicalPath = path,
                Name = Path.GetFileName(path),
                LastModified = DateTime.UtcNow,
            };

            AddFile(path, fileInfo);
        }

        public void AddFile(string path, TestFileInfo contents)
        {
            _lookup[path] = contents;
        }

        public void DeleteFile(string path)
        {
            _lookup.Remove(path);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (_lookup.ContainsKey(subpath))
            {
                return _lookup[subpath];
            }
            else
            {
                return new NotFoundFileInfo(subpath);
            }
        }

        public IExpirationTrigger Watch(string filter)
        {
            TestFileTrigger trigger;
            if (!_fileTriggers.TryGetValue(filter, out trigger) || trigger.IsExpired)
            {
                trigger = new TestFileTrigger();
                _fileTriggers[filter] = trigger;
            }

            return trigger;
        }

        public TestFileTrigger GetTrigger(string filter)
        {
            return _fileTriggers[filter];
        }
    }
}