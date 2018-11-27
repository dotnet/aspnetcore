// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace RazorWebSite
{
    public class UpdateableFileProvider : IFileProvider
    {
        private readonly Dictionary<string, TestFileInfo> _content = new Dictionary<string, TestFileInfo>()
        {
            {
                "/Views/UpdateableIndex/Index.cshtml",
                new TestFileInfo(@"@Html.Partial(""../UpdateableShared/_Partial.cshtml"")")
            },
            {
                "/Views/UpdateableShared/_Partial.cshtml",
                new TestFileInfo("Original content")
            },
        };

        public IDirectoryContents GetDirectoryContents(string subpath) => new NotFoundDirectoryContents();

        public void UpdateContent(string subpath, string content)
        {
            var old = _content[subpath];
            old.TokenSource.Cancel();
            _content[subpath] = new TestFileInfo(content);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            TestFileInfo fileInfo;
            if (!_content.TryGetValue(subpath, out fileInfo))
            {
                fileInfo = new TestFileInfo(null);
            }

            return fileInfo;
        }

        public IChangeToken Watch(string filter)
        {
            TestFileInfo fileInfo;
            if (_content.TryGetValue(filter, out fileInfo))
            {
                return fileInfo.ChangeToken;
            }

            return NullChangeToken.Singleton;
        }

        private class TestFileInfo : IFileInfo
        {
            private readonly string _content;

            public TestFileInfo(string content)
            {
                _content = content;
                ChangeToken = new CancellationChangeToken(TokenSource.Token);
                Exists = _content != null;
            }

            public bool Exists { get; }
            public bool IsDirectory => false;
            public DateTimeOffset LastModified => DateTimeOffset.MinValue;
            public long Length => -1;
            public string Name => null;
            public string PhysicalPath => null;
            public CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();
            public CancellationChangeToken ChangeToken { get; }

            public Stream CreateReadStream()
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(_content));
            }
        }
    }
}
