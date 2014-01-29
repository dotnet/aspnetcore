using System;
using System.IO;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class VirtualFile : IFileInfo
    {
        private readonly string _virtualPath;
        private readonly IFileInfo _underlyingFileInfo;

        public VirtualFile(string virtualPath, IFileInfo underlyingFileInfo)
        {
            _virtualPath = virtualPath;
            _underlyingFileInfo = underlyingFileInfo;
        }

        public Stream CreateReadStream()
        {
            return _underlyingFileInfo.CreateReadStream();
        }

        public bool IsDirectory
        {
            get { return _underlyingFileInfo.IsDirectory; }
        }

        public DateTime LastModified
        {
            get { return _underlyingFileInfo.LastModified; }
        }

        public long Length
        {
            get { return _underlyingFileInfo.Length; }
        }

        public string Name
        {
            get { return _underlyingFileInfo.Name; }
        }

        public string PhysicalPath
        {
            get { return _virtualPath; }
        }
    }
}
