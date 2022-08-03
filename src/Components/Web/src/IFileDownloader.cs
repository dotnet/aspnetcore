// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Facilitates triggering a file download on the client using data from .NET.
/// </summary>
public interface IFileDownloader
{
    /// <summary>
    /// Takes in a byte[] representing file data and triggers the file download on the client.
    /// </summary>
    /// <param name="fileName">A <see cref="string"/> that represents the default name of the file that will be downloaded.</param>
    /// <param name="data"> The <see cref="byte"/>[] data that will be downloaded by the client.</param>
    Task SaveAs(string fileName, byte[] data);

    /// <summary>
    /// Takes in a <see cref="Stream"/> representing file data and triggers the file download on the client.
    /// </summary>
    /// <param name="fileName">A <see cref="string"/> that represents the default name of the file that will be downloaded.</param>
    /// <param name="data"> The <see cref="Stream"/> data that will be downloaded by the client.</param>
    Task SaveAs(string fileName, Stream data);
}
