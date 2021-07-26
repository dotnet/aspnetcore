// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components
{
    /// <Summary>
    /// A stream that pulls each chunk on demand using JavaScript interop. This implementation is used for
    /// WebAssembly and WebView applications.
    /// </Summary>
    internal static class TransmitDataStreamToJS
    {
        internal static async Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference, Func<byte[], int, string?, Task> sendToJS)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(32 * 1024);

            try
            {
                int bytesRead;
                while ((bytesRead = await dotNetStreamReference.Stream.ReadAsync(buffer)) > 0)
                {
                    await sendToJS(buffer, bytesRead, null);
                }

                await sendToJS(Array.Empty<byte>(), 0, null);
            }
            catch (Exception ex)
            {
                try
                {
                    // Attempt to notify the client of the error.
                    await sendToJS(Array.Empty<byte>(), 0, ex.Message);
                }
                catch
                {
                    // JS Interop encountered an issue, unable to send error message to JS.
                }

                throw ex;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);

                if (!dotNetStreamReference.LeaveOpen)
                {
                    dotNetStreamReference.Stream?.Dispose();
                }
            }
        }

    }
}
