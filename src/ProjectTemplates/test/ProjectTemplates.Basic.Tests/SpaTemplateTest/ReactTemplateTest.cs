// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using ProjectTemplates.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ProjectTemplates.Basic.Tests.SpaTemplateTest
{
    public class ReactTemplateTest : SpaTemplateTestBase
    {
        public ReactTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
            : base(projectFactory, output)
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
