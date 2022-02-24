// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides extensions for HttpResponse exposing the SendFile extension.
/// </summary>
public static class SendFileResponseExtensions
{
    private const int StreamCopyBufferSize = 64 * 1024;

    /// <summary>
    /// Sends the given file using the SendFile extension.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="file">The file.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendFileAsync(this HttpResponse response, IFileInfo file, CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return SendFileAsyncCore(response, file, 0, null, cancellationToken);
    }

    /// <summary>
    /// Sends the given file using the SendFile extension.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="file">The file.</param>
    /// <param name="offset">The offset in the file.</param>
    /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendFileAsync(this HttpResponse response, IFileInfo file, long offset, long? count, CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return SendFileAsyncCore(response, file, offset, count, cancellationToken);
    }

    /// <summary>
    /// Sends the given file using the SendFile extension.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="fileName">The full path to the file.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendFileAsync(this HttpResponse response, string fileName, CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        if (fileName == null)
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        return SendFileAsyncCore(response, fileName, 0, null, cancellationToken);
    }

    /// <summary>
    /// Sends the given file using the SendFile extension.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="fileName">The full path to the file.</param>
    /// <param name="offset">The offset in the file.</param>
    /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendFileAsync(this HttpResponse response, string fileName, long offset, long? count, CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        if (fileName == null)
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        return SendFileAsyncCore(response, fileName, offset, count, cancellationToken);
    }

    private static async Task SendFileAsyncCore(HttpResponse response, IFileInfo file, long offset, long? count, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(file.PhysicalPath))
        {
            CheckRange(offset, count, file.Length);
            await using var fileContent = file.CreateReadStream();

            var useRequestAborted = !cancellationToken.CanBeCanceled;
            var localCancel = useRequestAborted ? response.HttpContext.RequestAborted : cancellationToken;

            try
            {
                localCancel.ThrowIfCancellationRequested();
                if (offset > 0)
                {
                    fileContent.Seek(offset, SeekOrigin.Begin);
                }
                await StreamCopyOperation.CopyToAsync(fileContent, response.Body, count, StreamCopyBufferSize, localCancel);
            }
            catch (OperationCanceledException) when (useRequestAborted) { }
        }
        else
        {
            await response.SendFileAsync(file.PhysicalPath, offset, count, cancellationToken);
        }
    }

    private static async Task SendFileAsyncCore(HttpResponse response, string fileName, long offset, long? count, CancellationToken cancellationToken = default)
    {
        var useRequestAborted = !cancellationToken.CanBeCanceled;
        var localCancel = useRequestAborted ? response.HttpContext.RequestAborted : cancellationToken;
        var sendFile = response.HttpContext.Features.GetRequiredFeature<IHttpResponseBodyFeature>();

        try
        {
            await sendFile.SendFileAsync(fileName, offset, count, localCancel);
        }
        catch (OperationCanceledException) when (useRequestAborted) { }
    }

    private static void CheckRange(long offset, long? count, long fileLength)
    {
        if (offset < 0 || offset > fileLength)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
        }
        if (count.HasValue &&
            (count.GetValueOrDefault() < 0 || count.GetValueOrDefault() > fileLength - offset))
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
        }
    }
}
