// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <inheritdoc />
/// <typeparam name="T">The <see cref="Type"/> of object that is going to be written in the response.</typeparam>
public class ProducesResponseTypeAttribute<T> : ProducesResponseTypeAttribute
{
    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    public ProducesResponseTypeAttribute(int statusCode) : base(typeof(T), statusCode) { }

    /// <summary>
    /// Initializes an instance of <see cref="ProducesResponseTypeAttribute"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP response status code.</param>
    /// <param name="contentType">The content type associated with the response.</param>
    /// <param name="additionalContentTypes">Additional content types supported by the response.</param>
    public ProducesResponseTypeAttribute(int statusCode, string contentType, params string[] additionalContentTypes)
            : base(typeof(T), statusCode, contentType, additionalContentTypes) { }
}
