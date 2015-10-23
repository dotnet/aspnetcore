// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FilteredDefaultAssemblyProviderFixture<TStartup> : MvcTestFixture<TStartup>
        where TStartup : new()
    {
        protected override void AddAdditionalServices(IServiceCollection services)
        {
            // TestHelper.CreateServer normally replaces the DefaultAssemblyProvider with a provider that limits the
            // set of candidate assemblies to the executing application. Switch it back to using a filtered default
            // assembly provider.
            services.AddTransient<IAssemblyProvider, FilteredDefaultAssemblyProvider>();
        }

        private class FilteredDefaultAssemblyProvider : DefaultAssemblyProvider
        {
            public FilteredDefaultAssemblyProvider(ILibraryManager libraryManager)
                : base(libraryManager)
            {
            }

            protected override IEnumerable<Library> GetCandidateLibraries()
            {
                var libraries = base.GetCandidateLibraries();

                // Filter out other WebSite projects
                return libraries.Where(library => !library.Name.Contains("WebSite") ||
                    library.Name.Equals(nameof(ControllerDiscoveryConventionsWebSite), StringComparison.Ordinal));
            }
        }
    }
}
