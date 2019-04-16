// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using ProjectTemplates.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ProjectTemplates.Basic.Tests.SpaTemplateTest
{
    public class AngularTemplateTest : SpaTemplateTestBase
    {
        public AngularTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
            : base(projectFactory, output) { }

        [Fact]
        public Task AngularTemplate_Works()
            => SpaTemplateImplAsync("angularnoauth", "angular", useLocalDb: false, usesAuth: false);

        [Fact]
        public Task AngularTemplate_IndividualAuth_Works()
            => SpaTemplateImplAsync("angularindividual", "angular", useLocalDb: false, usesAuth: true);

        [Fact]
        public Task AngularTemplate_IndividualAuth_Works_LocalDb()
            => SpaTemplateImplAsync("angularindividualuld", "angular", useLocalDb: true, usesAuth: true);
    }
}
