// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// An <see cref="IStatusCodeActionResult"/> that can be transformed to a more descriptive client error.
    /// </summary>
    public interface IClientErrorActionResult : IStatusCodeActionResult
    {
    }
}
