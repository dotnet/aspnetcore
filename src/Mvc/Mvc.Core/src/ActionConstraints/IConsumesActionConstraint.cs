// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

/// <summary>
/// An <see cref="IActionConstraint"/> constraint that identifies a type which can be used to select an action
/// based on incoming request.
/// </summary>
internal interface IConsumesActionConstraint : IActionConstraint
{
}
