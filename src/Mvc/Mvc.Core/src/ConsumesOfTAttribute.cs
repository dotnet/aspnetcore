// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <inheritdoc />
/// <typeparam name="T">The <see cref="Type"/> of object that is going to be read from the request.</typeparam>
public class ConsumesAttribute<T> : ConsumesAttribute
{
    /// <summary>
    /// Creates a new instance of <see cref="ConsumesAttribute{T}"/>.
    /// </summary>
    /// <param name="contentType">The request content type.</param>
    /// <param name="otherContentTypes">The additional list of allowed request content types.</param>
    public ConsumesAttribute(string contentType, params string[] otherContentTypes)
        : base(typeof(T), contentType, otherContentTypes)
    {
    }
}
