// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components;

/// <Summary>
/// A stream that pulls each chunk on demand using JavaScript interop. This implementation is used for
/// WebAssembly and WebView applications.
/// </Summary>
internal static class TransmitDataStreamToJS
{
    internal static async Task TransmitStreamAsync(IJSRuntime runtime, string methodIdentifier, long streamId, DotNetStreamReference dotNetStreamReference)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(32 * 1024);

        try
        {
            int bytesRead;
            while ((bytesRead = await dotNetStreamReference.Stream.ReadAsync(buffer)) > 0)
            {
                await runtime.InvokeVoidAsync(methodIdentifier, streamId, buffer, bytesRead, null);
            }

            // Notify client that the stream has completed
            await runtime.InvokeVoidAsync(methodIdentifier, streamId, Array.Empty<byte>(), 0, null);
        }
        catch (Exception ex)
        {
            try
            {
                // Attempt to notify the client of the error.
                await runtime.InvokeVoidAsync(methodIdentifier, streamId, Array.Empty<byte>(), 0, ex.Message);
            }
            catch
            {
                // JS Interop encountered an issue, unable to send error message to JS.
            }

            throw;
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
