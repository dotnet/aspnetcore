// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class WebApiTemplateTest : TemplateTestBase
    {
        public WebApiTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void WebApiTemplate()
        {
            RunDotNetNew("webapi");

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(publish))
                {
                    aspNetProcess.AssertOk("/api/values");
                    aspNetProcess.AssertNotFound("/");
                }
            }
        }
    }
}
