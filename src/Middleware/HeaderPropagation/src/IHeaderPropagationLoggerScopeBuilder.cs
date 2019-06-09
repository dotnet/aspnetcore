// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// A builder to build the <see cref="HeaderPropagationLoggerScope"/> for the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public interface IHeaderPropagationLoggerScopeBuilder
    {
        /// <summary>
        /// Build the <see cref="HeaderPropagationLoggerScope"/> for the current async context.
        /// </summary>
        internal HeaderPropagationLoggerScope Build();
    }
}
