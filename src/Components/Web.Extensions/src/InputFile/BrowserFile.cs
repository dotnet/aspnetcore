// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class BrowserFile : IBrowserFile
    {
        internal InputFile Owner { get; set; } = default!;

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime LastModified { get; set; }

        public long Size { get; set; }

        public string Type { get; set; } = string.Empty;

        public string? RelativePath { get; set; }

        public Stream OpenReadStream()
            => Owner.OpenReadStream(this);

        public Task<IBrowserFile> ToImageFileAsync(string format, int maxWidth, int maxHeight)
            => Owner.ConvertToImageFileAsync(this, format, maxWidth, maxHeight);
    }
}
