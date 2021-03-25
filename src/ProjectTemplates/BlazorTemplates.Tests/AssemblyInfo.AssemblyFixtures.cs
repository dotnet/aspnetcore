// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using ProjectTemplates.Tests.Infrastructure;
using Templates.Test;
using Templates.Test.Helpers;

[assembly: AssemblyFixture(typeof(ProjectFactoryFixture))]
[assembly: AssemblyFixture(typeof(PlaywrightFixture<BlazorServerTemplateTest>))]

