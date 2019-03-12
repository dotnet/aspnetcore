// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that asynchronously surrounds execution of all action results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="IAsyncAlwaysRunResultFilter"/> interface declares an <see cref="IAsyncResultFilter"/> implementation
    /// that should run for all action results. <seealso cref="IAsyncAlwaysRunResultFilter"/>.
    /// </para>
    /// <para>
    /// <see cref="IResultFilter"/> and <see cref="IAsyncResultFilter"/> instances are not executed in cases where
    /// an authorization filter or resource filter short-circuits the request to prevent execution of the action.
    /// <see cref="IResultFilter"/> and <see cref="IAsyncResultFilter"/> implementations
    /// are also not executed in cases where an exception filter handles an exception by producing an action result.
    /// </para>
    /// </remarks>
    public interface IAsyncAlwaysRunResultFilter : IAsyncResultFilter
    {
    }
}
