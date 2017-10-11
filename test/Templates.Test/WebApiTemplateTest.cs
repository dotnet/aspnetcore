// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class WebApiTemplateTest : TemplateTestBase
    {
        public WebApiTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void WebApiTemplate_Works_NetFramework()
            => WebApiTemplateImpl("net461");

        [Fact]
        public void WebApiTemplate_Works_NetCore()
            => WebApiTemplateImpl(null);

        private void WebApiTemplateImpl(string targetFrameworkOverride)
        {
            RunDotNetNew("api", targetFrameworkOverride);

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
                {
                    aspNetProcess.AssertOk("/api/values");
                    aspNetProcess.AssertNotFound("/");
                }
            }
        }
    }
}
