// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class MvcTestFixture<TStartup> : WebApplicationTestFixture<TStartup>
        where TStartup : class
    {
        public MvcTestFixture()
            : base(Path.Combine("test", "WebSites"))
        {
        }

        protected MvcTestFixture(string solutionRelativePath)
            : base(solutionRelativePath)
        {
        }

        protected override void ConfigureApplication(MvcWebApplicationBuilder<TStartup> builder)
        {
            base.ConfigureApplication(builder);
            builder.ApplicationAssemblies.Clear();
            builder.ApplicationAssemblies.Add(typeof(TStartup).GetTypeInfo().Assembly);
        }
    }
}
