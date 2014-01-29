﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    public interface IRouteEndpoint
    {
        Task Invoke(IDictionary<string, object> context);
    }
}
