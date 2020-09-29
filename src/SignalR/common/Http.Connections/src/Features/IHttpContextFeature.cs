// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Http.Connections.Features
{
    /// <summary>
    /// Feature set on the <see cref="ConnectionContext"/> that provides access to the <see cref="Http.HttpContext"/>
    /// associated with the connection if there is one.
    /// </summary>
    public interface IHttpContextFeature
    {
        /// <summary>
        /// The <see cref="Http.HttpContext"/> associated with the connection.
        /// </summary>
        HttpContext HttpContext { get; set; }
    }
}
