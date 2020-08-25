// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class BrowserFile : IBrowserFile
    {
        internal InputFile Owner { get; set; } = default!;

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTimeOffset LastModified { get; set; }

        public long Size { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public string? RelativePath { get; set; }

        public Stream OpenReadStream(CancellationToken cancellationToken = default)
            => Owner.OpenReadStream(this, cancellationToken);
    }
}
