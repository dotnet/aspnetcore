// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class FileListEntry : IFileListEntry
    {
        private Stream? _stream;

        internal InputFile Owner { get; set; } = default!;

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime? LastModified { get; set; }

        public long Size { get; set; }

        public string? Type { get; set; }

        public string? RelativePath { get; set; }

        public Stream Data
        {
            get
            {
                _stream ??= Owner.OpenFileStream(this);
                return _stream;
            }
        }

        public event EventHandler? OnDataRead;

        public Stream OpenFileStream()
            => Owner.OpenFileStream(this);

        public Task<IFileListEntry> ToImageFileAsync(string format, int maxWidth, int maxHeight)
            => Owner.ConvertToImageFileAsync(this, format, maxWidth, maxHeight);

        internal void InvokeOnDataRead()
            => OnDataRead?.Invoke(this, EventArgs.Empty);
    }
}
