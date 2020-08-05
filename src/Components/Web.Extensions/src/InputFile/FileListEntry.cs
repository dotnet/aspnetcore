// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class FileListEntry : IFileListEntry
    {
        private Stream? _stream;

        internal InputFile Owner { get; set; } = default!;

        public int Id { get; set; }

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

        internal void InvokeOnDataRead()
            => OnDataRead?.Invoke(this, EventArgs.Empty);
    }
}
