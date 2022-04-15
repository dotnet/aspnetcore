// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal static partial class ResultsCache
{
    public static NotFoundObjectHttpResult NotFound { get; } = new(null);
    public static UnauthorizedHttpResult Unauthorized { get; } = new();
    public static BadRequestObjectHttpResult BadRequest { get; } = new(null);
    public static ConflictObjectHttpResult Conflict { get; } = new(null);
    public static NoContentHttpResult NoContent { get; } = new();
    public static OkObjectHttpResult Ok { get; } = new(null);
    public static UnprocessableEntityObjectHttpResult UnprocessableEntity { get; } = new(null);
}
