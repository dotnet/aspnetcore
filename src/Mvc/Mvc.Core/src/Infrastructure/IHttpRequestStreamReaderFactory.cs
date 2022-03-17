// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Creates <see cref="TextReader"/> instances for reading from <see cref="Http.HttpRequest.Body"/>.
/// </summary>
public interface IHttpRequestStreamReaderFactory
{
    /// <summary>
    /// Creates a new <see cref="TextReader"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/>, usually <see cref="Http.HttpRequest.Body"/>.</param>
    /// <param name="encoding">The <see cref="Encoding"/>, usually <see cref="Encoding.UTF8"/>.</param>
    /// <returns>A <see cref="TextReader"/>.</returns>
    TextReader CreateReader(Stream stream, Encoding encoding);
}
