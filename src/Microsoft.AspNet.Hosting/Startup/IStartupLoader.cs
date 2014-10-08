// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;

namespace Microsoft.AspNet.Hosting.Startup
{
    public interface IStartupLoader
    {
        Action<IApplicationBuilder> LoadStartup(
            string applicationName, 
            string environmentName,
            IList<string> diagnosticMessages);
    }
}
