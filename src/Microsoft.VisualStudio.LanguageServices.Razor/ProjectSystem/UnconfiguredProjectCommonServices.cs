// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.References;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // This defines the set of services that we frequently need for working with UnconfiguredProject.
    //
    // We're following a somewhat common pattern for code that uses CPS. It's really easy to end up
    // relying on service location inside CPS, which can be hard to test. This approach makes it easy
    // for us to build reusable mocks instead.
    internal interface IUnconfiguredProjectCommonServices
    {
        ConfiguredProject ActiveConfiguredProject { get; }

        IAssemblyReferencesService ActiveConfiguredProjectAssemblyReferences { get; }

        IPackageReferencesService ActiveConfiguredProjectPackageReferences { get; }

        Rules.RazorProjectProperties ActiveConfiguredProjectRazorProperties { get; }

        IActiveConfiguredProjectSubscriptionService ActiveConfiguredProjectSubscription { get; }
        
        IProjectThreadingService ThreadingService { get; }

        UnconfiguredProject UnconfiguredProject { get; }
    }
}
