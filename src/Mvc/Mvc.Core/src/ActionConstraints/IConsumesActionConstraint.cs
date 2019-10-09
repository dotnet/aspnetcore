// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ActionConstraints
{
    /// <summary>
    /// An <see cref="IActionConstraint"/> constraint that identifies a type which can be used to select an action
    /// based on incoming request.
    /// </summary>
    internal interface IConsumesActionConstraint : IActionConstraint
    {
    }
}
