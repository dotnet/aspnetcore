// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Defines the contract to convert a type to an <see cref="IActionResult"/> during action invocation.
    /// </summary>
    public interface IConvertToActionResult
    {
        /// <summary>
        /// Converts the current instance to an instance of <see cref="IActionResult"/>.
        /// </summary>
        /// <returns>The converted <see cref="IActionResult"/>.</returns>
        IActionResult Convert();
    }
}
