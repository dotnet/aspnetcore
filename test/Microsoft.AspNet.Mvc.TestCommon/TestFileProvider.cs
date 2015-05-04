// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.Framework.Caching;

namespace Microsoft.AspNet.Mvc.Razor
{
    internal class TestFileProvider : IFileProvider
    {
        private readonly Dictionary<string, IFileInfo> _lookup =
            new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, TestFileTrigger> _fileTriggers =
            new Dictionary<string, TestFileTrigger>(StringComparer.Ordinal);

        public virtual IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotSupportedException();
        }

        public TestFileInfo AddFile(string path, string contents)
        {
            var fileInfo = new TestFileInfo
            {
                Content = contents,
                PhysicalPath = path,
                Name = Path.GetFileName(path),
                LastModified = DateTime.UtcNow,
            };

            AddFile(path, fileInfo);

            return fileInfo;
        }

        public void AddFile(string path, IFileInfo contents)
        {
            _lookup[path] = contents;
        }

        public void DeleteFile(string path)
        {
            _lookup.Remove(path);
        }

        public virtual IFileInfo GetFileInfo(string subpath)
        {
            if (_lookup.ContainsKey(subpath))
            {
                return _lookup[subpath];
            }
            else
            {
                return new NotFoundFileInfo();
            }
        }

        public virtual IExpirationTrigger Watch(string filter)
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

        private class NotFoundFileInfo : IFileInfo
        {
            public bool Exists
            {
                get
                {
                    return false;
                }
            }

            public bool IsDirectory
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public DateTimeOffset LastModified
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Name
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string PhysicalPath
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Stream CreateReadStream()
            {
                throw new NotImplementedException();
            }
        }
    }
}