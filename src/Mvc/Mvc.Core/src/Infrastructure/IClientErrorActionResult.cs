// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// An <see cref="IStatusCodeActionResult"/> that can be transformed to a more descriptive client error.
/// </summary>
public interface IClientErrorActionResult : IStatusCodeActionResult
{
}
