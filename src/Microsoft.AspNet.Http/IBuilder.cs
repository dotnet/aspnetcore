// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Builder
{
    public interface IBuilder
    {
        IServiceProvider ApplicationServices { get; set; }

        IServerInformation Server { get; set; }

        IDictionary<string, object> Properties { get; set; }

        IBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);

        IBuilder New();

        RequestDelegate Build();
    }
}
