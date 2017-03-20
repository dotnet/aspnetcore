// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class TestFileChangeToken : IChangeToken
    {
        public bool HasChanged => throw new NotImplementedException();

        public bool ActiveChangeCallbacks => throw new NotImplementedException();

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }

    public class TestFileInfo : IFileInfo
    {
        private string _content;

        public bool IsDirectory { get; } = false;

        public DateTimeOffset LastModified { get; set; }

        public long Length { get; set; }

        public string Name { get; set; }

        public string PhysicalPath { get; set; }

        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                Length = Encoding.UTF8.GetByteCount(Content);
            }
        }

        public bool Exists
        {
            get { return true; }
        }

        public Stream CreateReadStream()
        {
            var bytes = Encoding.UTF8.GetBytes(Content);
            return new MemoryStream(bytes);
        }
    }

    public class TestFileProvider : IFileProvider
    {
        private readonly Dictionary<string, IFileInfo> _lookup =
            new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, IDirectoryContents> _directoryContentsLookup =
            new Dictionary<string, IDirectoryContents>();

        private readonly Dictionary<string, TestFileChangeToken> _fileTriggers =
            new Dictionary<string, TestFileChangeToken>(StringComparer.Ordinal);

        public virtual IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (_directoryContentsLookup.TryGetValue(subpath, out var value))
            {
                return value;
            }

            return new NotFoundDirectoryContents();
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

        public virtual IChangeToken Watch(string filter)
        {
            TestFileChangeToken changeToken;
            if (!_fileTriggers.TryGetValue(filter, out changeToken) || changeToken.HasChanged)
            {
                changeToken = new TestFileChangeToken();
                _fileTriggers[filter] = changeToken;
            }

            return changeToken;
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
