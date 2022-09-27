// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the collection of files sent with the HttpRequest.
/// </summary>
public interface IFormFileCollection : IReadOnlyList<IFormFile>
{
    /// <summary>
    /// Gets the first file with the specified name.
    /// </summary>
    /// <param name="name">The name of the file to get.</param>
    /// <returns>
    ///	The requested file, or null if it is not present.
    /// </returns>
    IFormFile? this[string name] { get; }

    /// <summary>
    /// Gets the first file with the specified name.
    /// </summary>
    /// <param name="name">The name of the file to get.</param>
    /// <returns>
    ///	The requested file, or null if it is not present.
    /// </returns>
    IFormFile? GetFile(string name);

    /// <summary>
    ///     Gets an <see cref="IReadOnlyList{T}" /> containing the files of the
    ///     <see cref="IFormFileCollection" /> with the specified name.
    /// </summary>
    /// <param name="name">The name of the files to get.</param>
    /// <returns>
    ///     An <see cref="IReadOnlyList{T}" /> containing the files of the object
    ///     that implements <see cref="IFormFileCollection" />.
    /// </returns>
    IReadOnlyList<IFormFile> GetFiles(string name);
}
