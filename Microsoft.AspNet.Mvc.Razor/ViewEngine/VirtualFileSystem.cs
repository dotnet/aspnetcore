using System;
using System.Collections.Generic;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class VirtualFileSystem : IFileSystem
    {
        private readonly IFileSystem _fileSystem;

        public VirtualFileSystem(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
        {
            string translated = TranslatePath(subpath);
            return _fileSystem.TryGetFileInfo(translated, out fileInfo);
        }

        public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
        {
            string translated = TranslatePath(subpath);
            return _fileSystem.TryGetDirectoryContents(translated, out contents);
        }

        private static string TranslatePath(string path)
        {
            if (path.StartsWith("~/", StringComparison.Ordinal))
            {
                path = path.Substring(2);
            }
            return path;
        }
    }
}
