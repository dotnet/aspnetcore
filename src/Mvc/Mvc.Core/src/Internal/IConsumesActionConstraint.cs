// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// An <see cref="IActionConstraint"/> constraint that identifies a type which can be used to select an action
    /// based on incoming request.
    /// </summary>
    public interface IConsumesActionConstraint : IActionConstraint
    {
    }
}