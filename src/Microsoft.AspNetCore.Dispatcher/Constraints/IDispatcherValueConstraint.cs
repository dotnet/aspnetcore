// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Defines the contract that a class must implement in order to check whether a URL parameter
    /// value is valid for a constraint.
    /// </summary>
    public interface IDispatcherValueConstraint
    {
        /// <summary>
        /// Determines whether the current operation should succeed or fail, typically by validating one or
        /// more values in <see cref="DispatcherValueConstraintContext.Values"/>.
        /// </summary>
        /// <param name="context">The <see cref="DispatcherValueConstraintContext"/> associated with the current operation.</param>
        /// <returns><c>true</c> if the current operation should proceed; otherwise <c>false</c>.</returns>
        bool Match(DispatcherValueConstraintContext context);
    }
}

