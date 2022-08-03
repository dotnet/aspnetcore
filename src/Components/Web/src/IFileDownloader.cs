// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents the file data from a <see cref="FileDownloader"/> component.
/// </summary>
public interface IFileDownloader
{
    /// <summary>
    /// Takes in a byte[] representing file data and converts it into a MemoryStream.
    /// </summary>
    /// <param name="fileName">A <see cref="string"/> that contains the specified file name.</param>
    /// <param name="data"> The <see cref="byte[]"/> data that will be converted into a <see cref="MemoryStream"/>.</param>
    Task SaveAs(string fileName, byte[] data);

    /// <summary>
    /// Takes in a Stream representing file data, converts it into a DotNetStreamReference, and invokes JS to save the file to the specified file name..
    /// </summary>
    /// <param name="fileName">A <see cref="string"/> that contains the specified file name.</param>
    /// <param name="data"> The <see cref="Stream"/> data that is converted and streamed to the client.</param>
    Task SaveAs(string fileName, Stream data);
}
