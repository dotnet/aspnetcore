// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class DefaultCircuitFactoryOptions
    {
        // During the DI configuration phase, we use Configure<DefaultCircuitFactoryOptions>(...)
        // callbacks to build up this dictionary mapping paths to startup actions
        internal Dictionary<PathString, Action<IComponentsApplicationBuilder>> StartupActions { get; }
            = new Dictionary<PathString, Action<IComponentsApplicationBuilder>>();
    }
}
