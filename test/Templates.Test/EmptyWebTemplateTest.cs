// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class EmptyWebTemplateTest : TemplateTestBase
    {
        public EmptyWebTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void EmptyWebTemplate_Works_NetFramework()
            => EmptyWebTemplateImpl("net461");

        [Fact]
        public void EmptyWebTemplate_Works_NetCore()
            => EmptyWebTemplateImpl(null);

        private void EmptyWebTemplateImpl(string targetFrameworkOverride)
        {
            RunDotNetNew("web", targetFrameworkOverride);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/");
                }
            }
        }
    }
}
