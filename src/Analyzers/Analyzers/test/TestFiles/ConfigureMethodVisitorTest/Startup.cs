// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
