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
    /// Feature set on the <see cref="ConnectionContext"/> that exposes the <see cref="HttpTransportType"/>
    /// the connection is using.
    /// </summary>
    public interface IHttpTransportFeature
    {
        /// <summary>
        /// The <see cref="HttpTransportType"/> the connection is using.
        /// </summary>
        HttpTransportType TransportType { get; }
    }
}
