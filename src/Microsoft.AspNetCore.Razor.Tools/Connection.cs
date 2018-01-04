// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.TagHelperTool
{
    internal abstract class Connection : IDisposable
    {
        public string Identifier { get; protected set; }

        public Stream Stream { get; protected set; }

        public abstract Task WaitForDisconnectAsync(CancellationToken cancellationToken);

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
