// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    /// <summary>
    /// A delegate which asyncronously returns a <see cref="ResourceExecutedContext"/>.
    /// </summary>
    /// <returns>A <see cref="ResourceExecutedContext"/>.</returns>
    public delegate Task<ResourceExecutedContext> ResourceExecutionDelegate();
}