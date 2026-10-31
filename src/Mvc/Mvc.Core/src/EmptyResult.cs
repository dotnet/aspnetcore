// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents an <see cref="ActionResult"/> that when executed will
/// do nothing.
/// </summary>
public class EmptyResult : ActionResult
{
    /// <inheritdoc />
    public override void ExecuteResult(ActionContext context)
    {
    }
}
