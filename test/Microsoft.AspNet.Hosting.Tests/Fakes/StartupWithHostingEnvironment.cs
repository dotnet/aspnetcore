using System;
using Microsoft.AspNet.Builder;

namespace Microsoft.AspNet.Hosting.Tests.Fakes
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