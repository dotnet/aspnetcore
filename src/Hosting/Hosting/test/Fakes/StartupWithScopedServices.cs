// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using static Microsoft.AspNetCore.Hosting.Tests.StartupManagerTests;

namespace Microsoft.AspNetCore.Hosting.Fakes
{
    public class StartupWithScopedServices
    {
        public DisposableService DisposableService { get; set; }

        public void Configure(IApplicationBuilder builder, DisposableService disposable)
        {
            DisposableService = disposable;
        }
    }
}
