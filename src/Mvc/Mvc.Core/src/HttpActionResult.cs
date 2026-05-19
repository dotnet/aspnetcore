// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that when executed will produce a response based on the <see cref="IResult"/> provided.
/// </summary>
internal sealed class HttpActionResult : ActionResult
{
    /// <summary>
    /// Gets the instance of the current <see cref="IResult"/>.
    /// </summary>
    public IResult Result { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpActionResult"/> class with the
    /// <see cref="IResult"/> provided.
    /// </summary>
    /// <param name="result">The <see cref="IResult"/> instance to be used during the <see cref="ExecuteResultAsync"/> invocation.</param>
    public HttpActionResult(IResult result)
    {
        Result = result;
    }

    /// <inheritdoc/>
    public override Task ExecuteResultAsync(ActionContext context)
        => Result.ExecuteAsync(context.HttpContext);
}
