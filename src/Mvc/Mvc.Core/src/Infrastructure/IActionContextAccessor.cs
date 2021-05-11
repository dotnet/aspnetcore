// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Defines an interface for exposing an <see cref="ActionContext"/>.
    /// </summary>
    public interface IActionContextAccessor
    {
        /// <summary>
        /// Gets or sets the <see cref="ActionContext"/>.
        /// </summary>
        [DisallowNull]
        ActionContext? ActionContext { get; set; }
    }
}
