// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A marker interface for <see cref="IActionResult"/> types which need to have temp data saved.
/// </summary>
public interface IKeepTempDataResult : IActionResult
{
}
