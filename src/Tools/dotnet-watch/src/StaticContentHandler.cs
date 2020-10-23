// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class StaticContentHandler
    {
        private static readonly byte[] UpdateCssMessage = Encoding.UTF8.GetBytes("UpdateStaticFile||");

        internal static async ValueTask TryHandleFileAction(BrowserRefreshServer browserRefreshServer, FileItem fileItem, CancellationToken cancellationToken)
        {
            var filePath = fileItem.StaticWebAssetPath;
            var bytesToRent = UpdateCssMessage.Length + Encoding.UTF8.GetMaxByteCount(filePath.Length + 2);
            var bytes = ArrayPool<byte>.Shared.Rent(bytesToRent);

            try
            {
                UpdateCssMessage.CopyTo(bytes.AsSpan());
                var length = UpdateCssMessage.Length;
                length += Encoding.UTF8.GetBytes(filePath, bytes.AsSpan(length));

                await browserRefreshServer.SendMessage(bytes.AsMemory(0, length), cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
    }
}
