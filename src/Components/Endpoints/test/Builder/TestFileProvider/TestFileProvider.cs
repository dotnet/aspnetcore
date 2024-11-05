// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders;

public class TestFileProvider : IFileProvider
{
    private readonly Dictionary<string, IFileInfo> _lookup =
        new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
    private readonly Dictionary<string, IDirectoryContents> _directoryContentsLookup =
        new Dictionary<string, IDirectoryContents>();

    private readonly Dictionary<string, TestFileChangeToken> _fileTriggers =
        new Dictionary<string, TestFileChangeToken>(StringComparer.Ordinal);

    public TestFileProvider() : this(string.Empty)
    {
    }

    public TestFileProvider(string root)
    {
        Root = root;
    }

    public string Root { get; }

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
            PhysicalPath = Path.Combine(Root, NormalizeAndEnsureValidPhysicalPath(path)),
            Name = Path.GetFileName(path),
            LastModified = DateTime.UtcNow,
        };

        AddFile(path, fileInfo);

        return fileInfo;
    }

    public TestDirectoryContent AddDirectoryContent(string path, IEnumerable<IFileInfo> files)
    {
        var directoryContent = new TestDirectoryContent(Path.GetFileName(path), files);
        _directoryContentsLookup[path] = directoryContent;
        return directoryContent;
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
        if (_lookup.TryGetValue(subpath, out var fileInfo))
        {
            return fileInfo;
        }
        else
        {
            return new NotFoundFileInfo();
        }
    }

    public virtual TestFileChangeToken AddChangeToken(string filter)
    {
        var changeToken = new TestFileChangeToken(filter);
        _fileTriggers[filter] = changeToken;

        return changeToken;
    }

    public virtual IChangeToken Watch(string filter)
    {
        if (!_fileTriggers.TryGetValue(filter, out var changeToken) || changeToken.HasChanged)
        {
            changeToken = new TestFileChangeToken(filter);
            _fileTriggers[filter] = changeToken;
        }

        return changeToken;
    }

    public TestFileChangeToken GetChangeToken(string filter)
    {
        return _fileTriggers[filter];
    }

    private static string NormalizeAndEnsureValidPhysicalPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return filePath;
        }

        filePath = filePath.Replace('/', Path.DirectorySeparatorChar);

        if (filePath[0] == Path.DirectorySeparatorChar)
        {
            filePath = filePath.Substring(1);
        }

        return filePath;
    }

    private sealed class NotFoundFileInfo : IFileInfo
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
