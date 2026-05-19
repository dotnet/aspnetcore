// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.WebView;

public class StaticContentProviderTests
{
    [Fact]
    public void TryGetResponseContentReturnsCorrectContentTypeForNonPhysicalFile()
    {
        // Arrange
        const string cssFilePath = "folder/file.css";
        const string cssFileContent = "this is css";
        var inMemoryFileProvider = new InMemoryFileProvider(
            new Dictionary<string, string>
            {
                    { cssFilePath, cssFileContent },
            });
        var appBase = "fake://0.0.0.0/";
        var scp = new StaticContentProvider(inMemoryFileProvider, new Uri(appBase), "fakehost.html");

        // Act
        Assert.True(scp.TryGetResponseContent(
            requestUri: appBase + cssFilePath,
            allowFallbackOnHostPage: false,
            out var statusCode,
            out var statusMessage,
            out var content,
            out var headers));

        // Assert
        var contentString = new StreamReader(content).ReadToEnd();
        Assert.Equal(200, statusCode);
        Assert.Equal("OK", statusMessage);
        Assert.Equal("this is css", contentString);
        Assert.True(headers.TryGetValue("Content-Type", out var contentTypeValue));
        Assert.Equal("text/css", contentTypeValue);
    }

    private sealed class InMemoryFileProvider : IFileProvider
    {
        public InMemoryFileProvider(IDictionary<string, string> filePathsAndContents)
        {
            ArgumentNullException.ThrowIfNull(filePathsAndContents);

            FilePathsAndContents = filePathsAndContents;
        }

        public IDictionary<string, string> FilePathsAndContents { get; }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return new InMemoryDirectoryContents(this, subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return FilePathsAndContents
                .Where(kvp => kvp.Key == subpath)
                .Select(x => new InMemoryFileInfo(x.Key, x.Value))
                .Single();
        }

        public IChangeToken Watch(string filter)
        {
            return null;
        }

        private sealed class InMemoryDirectoryContents : IDirectoryContents
        {
            private readonly InMemoryFileProvider _inMemoryFileProvider;
            private readonly string _subPath;

            public InMemoryDirectoryContents(InMemoryFileProvider inMemoryFileProvider, string subPath)
            {
                _inMemoryFileProvider = inMemoryFileProvider ?? throw new ArgumentNullException(nameof(inMemoryFileProvider));
                _subPath = subPath ?? throw new ArgumentNullException(nameof(inMemoryFileProvider));
            }

            public bool Exists => true;

            public IEnumerator<IFileInfo> GetEnumerator()
            {
                return
                    _inMemoryFileProvider
                        .FilePathsAndContents
                        .Where(kvp => kvp.Key.StartsWith(_subPath, StringComparison.Ordinal))
                        .Select(x => new InMemoryFileInfo(x.Key, x.Value))
                        .GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class InMemoryFileInfo : IFileInfo
        {
            private readonly string _filePath;
            private readonly string _fileContents;

            public InMemoryFileInfo(string filePath, string fileContents)
            {
                _filePath = filePath;
                _fileContents = fileContents;
            }

            public bool Exists => true;

            public long Length => _fileContents.Length;

            public string PhysicalPath => null;

            public string Name => Path.GetFileName(_filePath);

            public DateTimeOffset LastModified => DateTimeOffset.Now;

            public bool IsDirectory => false;

            public Stream CreateReadStream()
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(_fileContents));
            }
        }
    }
}
