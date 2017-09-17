// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// A delegate which attempts to create a <see cref="Func{RequestDelegate, RequestDelegate}"/> for the selected <see cref="Endpoint"/>.
    /// </summary>
    /// <param name="endpoint">The <see cref="Endpoint"/> selected by the dispatcher.</param>
    /// <returns>
    /// A <see cref="Func{RequestDelegate, RequestDelegate}"/>  that invokes the operation represented by the <see cref="Endpoint"/>, or <c>null</c>.
    /// </returns>
    public delegate Func<RequestDelegate, RequestDelegate> EndpointHandlerFactory(Endpoint endpoint);
}
