// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace RazorBuildWebSite;

public class UpdateableFileProvider : IFileProvider
{
    public CancellationTokenSource _pagesTokenSource = new CancellationTokenSource();

    private readonly Dictionary<string, TestFileInfo> _content = new Dictionary<string, TestFileInfo>()
        {
            {
                "/Views/UpdateableViews/_ViewImports.cshtml",
                new TestFileInfo(string.Empty)
            },
            {
                "/Views/UpdateableViews/Index.cshtml",
                new TestFileInfo("Original content")
            },
            {
                "/Pages/UpdateablePage.cshtml",
                new TestFileInfo("@page" + Environment.NewLine + "Original content")
            },
        };

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        if (subpath == "/Pages")
        {
            return new PagesDirectoryContents();
        }

        return new NotFoundDirectoryContents();
    }

    public void UpdateContent(string subpath, string content)
    {
        var old = _content[subpath];
        old.TokenSource.Cancel();
        _content[subpath] = new TestFileInfo(content);
    }

    public void CancelRazorPages()
    {
        var oldToken = _pagesTokenSource;
        _pagesTokenSource = new CancellationTokenSource();
        oldToken.Cancel();
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        if (!_content.TryGetValue(subpath, out var fileInfo))
        {
            fileInfo = new TestFileInfo(null);
        }

        return fileInfo;
    }

    public IChangeToken Watch(string filter)
    {
        if (filter == "/Pages/**/*.cshtml")
        {
            return new CancellationChangeToken(_pagesTokenSource.Token);
        }

        if (_content.TryGetValue(filter, out var fileInfo))
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
        public string Name { get; set; }
        public string PhysicalPath => null;
        public CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();
        public CancellationChangeToken ChangeToken { get; }

        public Stream CreateReadStream()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(_content));
        }
    }

    private class PagesDirectoryContents : IDirectoryContents
    {
        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            var file = new TestFileInfo("@page" + Environment.NewLine + "Original content")
            {
                Name = "UpdateablePage.cshtml"
            };

            var files = new List<IFileInfo> { file };
            return files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
