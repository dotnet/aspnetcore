// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using ProjectTemplates.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ProjectTemplates.Basic.Tests.SpaTemplateTest
{
    public class ReactReduxTemplateTest : SpaTemplateTestBase
    {
        public ReactReduxTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
            : base(projectFactory, output)
        {
        }

        [Fact]
        public Task ReactReduxTemplate_Works_NetCore()
            => SpaTemplateImplAsync("reactredux", "reactredux", useLocalDb: false, usesAuth: false);
    }
}
