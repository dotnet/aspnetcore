// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.WebEncoders;
using Microsoft.Framework.WebEncoders.Testing;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class MvcEncodedTestFixture<TStartup> : MvcTestFixture<TStartup>
        where TStartup : new()
    {
        protected override void AddAdditionalServices(IServiceCollection services)
        {
            services.AddTransient<IHtmlEncoder, CommonTestEncoder>();
            services.AddTransient<IJavaScriptStringEncoder, CommonTestEncoder>();
            services.AddTransient<IUrlEncoder, CommonTestEncoder>();
        }
    }
}
