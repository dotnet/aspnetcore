// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal sealed class CreatedHttpResult : ObjectAtLocationHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the content has been created.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public CreatedHttpResult(string location, object? value)
        : base(location, value, StatusCodes.Status201Created)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the content has been created.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public CreatedHttpResult(Uri location, object? value)
        : base(location, value, StatusCodes.Status201Created)
    {
    }
}
