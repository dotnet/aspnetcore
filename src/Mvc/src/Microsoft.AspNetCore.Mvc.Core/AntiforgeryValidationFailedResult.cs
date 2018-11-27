// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Core.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="BadRequestResult"/> used for antiforgery validation
    /// failures. Use <see cref="IAntiforgeryValidationFailedResult"/> to
    /// match for validation failures inside MVC result filters.
    /// </summary>
    public class AntiforgeryValidationFailedResult : BadRequestResult, IAntiforgeryValidationFailedResult
    { }
}
