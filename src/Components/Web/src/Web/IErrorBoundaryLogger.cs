// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web
{
    // The rules arround logging differ between environments.
    // - For WebAssembly, we always log to the console with detailed information
    // - For Server, we log to both the server-side log (with detailed information), and to the
    //   client (respecting the DetailedError option)
    // - In prerendering, we log only to the server-side log

    /// <summary>
    /// Logs exception information for a <see cref="ErrorBoundary"/> component.
    /// </summary>
    public interface IErrorBoundaryLogger
    {
        /// <summary>
        /// Logs the supplied <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log.</param>
        /// <param name="clientOnly">If true, indicates that the error should only be logged to the client (e.g., because it was already logged to the server).</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        ValueTask LogErrorAsync(Exception exception, bool clientOnly);
    }
}
