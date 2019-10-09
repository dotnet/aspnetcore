// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class ReactTemplateTest : SpaTemplateTestBase
    {
        public ReactTemplateTest(ProjectFactoryFixture projectFactory, BrowserFixture browserFixture, ITestOutputHelper output)
            : base(projectFactory, browserFixture, output)
        {
        }

        [Fact]
        public Task ReactTemplate_Works_NetCore()
            => SpaTemplateImplAsync("reactnoauth", "react", useLocalDb: false, usesAuth: false);

        [Fact]
        public Task ReactTemplate_IndividualAuth_NetCore()
            => SpaTemplateImplAsync("reactindividual", "react", useLocalDb: false, usesAuth: true);

        [Fact]
        public Task ReactTemplate_IndividualAuth_NetCore_LocalDb()
            => SpaTemplateImplAsync("reactindividualuld", "react", useLocalDb: true, usesAuth: true);
    }
}
