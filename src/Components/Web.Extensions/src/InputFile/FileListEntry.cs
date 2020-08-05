using System;
using System.IO;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class FileListEntry : IFileListEntry
    {
        private Stream? _stream;

        internal InputFile Owner { get; set; } = default!;

        public string? Name { get; set; }

        public DateTime? LastModified { get; set; }

        public int Size { get; set; }

        public string? Type { get; set; }

        public Stream Data
        {
            get
            {
                _stream ??= Owner.OpenFileStream(this);
                return _stream;
            }
        }

        public event EventHandler? OnDataRead;
    }
}
