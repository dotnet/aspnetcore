// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// An interface for components that can select an <see cref="Endpoint"/> given the current request, as part
    /// of the execution of <see cref="DispatcherMiddleware"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IMatcher"/> implementations can also optionally implement the <see cref="IEndpointCollectionProvider"/>
    /// and <see cref="IAddressCollectionProvider"/> interfaces to provide addition information.
    /// </para>
    /// <para>
    /// Use <see cref="DispatcherOptions"/> to register instances of <see cref="IMatcher"/> that will be used by the
    /// <see cref="DispatcherMiddleware"/>.
    /// </para>
    /// </remarks>
    public interface IMatcher
    {
        /// <summary>
        /// Attempts to asynchronously select an <see cref="Endpoint"/> for the current request.
        /// </summary>
        /// <param name="context">The <see cref="MatcherContext"/> associated with the current request.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous completion of the operation.</returns>
        /// <remarks>
        /// <para>
        /// An implementation should use data from the current request (<see cref="MatcherContext.HttpContext"/>) to select the
        /// <see cref="Endpoint"/> and set <see cref="MatcherContext.Endpoint"/> and optionally <see cref="MatcherContext.Values"/>
        /// to indicate a successful result.
        /// </para>
        /// <para>
        /// If the matcher encounters an immediate failure condition, the implementation should set
        /// <see cref="MatcherContext.ShortCircuit"/> to a <see cref="RequestDelegate"/> that will respond to the current request.
        /// </para>
        /// </remarks>
        Task MatchAsync(MatcherContext context);
    }
}
