// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.E2ETesting;
using Xunit;

[assembly: TestFramework("Microsoft.AspNetCore.E2ETesting.XunitTestFrameworkWithAssemblyFixture", "Microsoft.AspNetCore.Components.E2ETests")]
[assembly: AssemblyFixture(typeof(SeleniumStandaloneServer))]
