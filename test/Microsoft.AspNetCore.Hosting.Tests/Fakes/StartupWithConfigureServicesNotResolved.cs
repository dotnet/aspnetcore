// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Fakes
{
    public class StartupWithConfigureServicesNotResolved
    {
        public StartupWithConfigureServicesNotResolved()
        {
        }

        public void Configure(IApplicationBuilder builder, int notAService)
        {
        }
    }
}
