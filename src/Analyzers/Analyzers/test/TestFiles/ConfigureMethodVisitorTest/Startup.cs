// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

public class GlobalStartup
{
    public void Configure(IApplicationBuilder app)
    {
    }
}

namespace Another
{
    public class AnotherStartup
    {
        public void Configure(IApplicationBuilder app)
        {
        }
    }
}

namespace ANamespace
{
    public class Startup
    {
        public void ConfigureDevelopment(IApplicationBuilder app)
        {
        }

        public class NestedStartup
        {
            public void ConfigureTest(IApplicationBuilder app)
            {
            }
        }
    }
}

namespace ANamespace.Nested
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
        }

        public class NestedStartup
        {
            public void Configure(IApplicationBuilder app)
            {
            }
        }
    }
}
