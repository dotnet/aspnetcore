// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Core.Infrastructure
{
    /// <summary>
    /// Represents an <see cref="IActionResult"/> that is used when the
    /// antiforgery validation failed. This can be matched inside MVC result
    /// filters to process the validation failure.
    /// </summary>
    public interface IAntiforgeryValidationFailedResult : IActionResult
    { }
}