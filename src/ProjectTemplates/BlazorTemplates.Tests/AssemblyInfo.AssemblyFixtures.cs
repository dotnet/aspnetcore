// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;

[assembly: TestFramework("Microsoft.AspNetCore.E2ETesting.XunitTestFrameworkWithAssemblyFixture", "BlazorTemplates.Tests")]

[assembly: Microsoft.AspNetCore.E2ETesting.AssemblyFixture(typeof(ProjectFactoryFixture))]
[assembly: Microsoft.AspNetCore.E2ETesting.AssemblyFixture(typeof(SeleniumStandaloneServer))]

[assembly: QuarantinedTest("Investigation pending in https://github.com/dotnet/aspnetcore/issues/20479")]
