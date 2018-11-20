using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes
{
    public class StartupWithHostingEnvironment
    {
        public StartupWithHostingEnvironment(IHostingEnvironment env)
        {
            env.EnvironmentName = "Changed";
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

        }
    }
}