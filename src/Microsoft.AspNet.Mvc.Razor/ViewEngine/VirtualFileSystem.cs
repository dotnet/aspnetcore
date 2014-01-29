using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class VirtualFileSystem : IVirtualFileSystem
    {
        private readonly IFileSystem _fileSystem;

        public VirtualFileSystem(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
        {
            string translated = TranslatePath(subpath);
            if (_fileSystem.TryGetFileInfo(translated, out fileInfo))
            {
                fileInfo = new VirtualFile(subpath, fileInfo);
            }
            return false;
        }

        public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
        {
            string translated = TranslatePath(subpath);
            if (_fileSystem.TryGetDirectoryContents(translated, out contents))
            {
                contents = contents.Select(c => new VirtualFile(subpath + '/' + c.Name, c));
            }
            return false;
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
