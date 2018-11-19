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

        [Fact]
        public void EmptyWebTemplate()
        {
            RunDotNetNew("web");

            foreach (var publish in new[] { false, true })
            {
                using (var aspNetProcess = StartAspNetProcess(publish))
                {
                    aspNetProcess.AssertOk("/");
                }
            }
        }
    }
}
