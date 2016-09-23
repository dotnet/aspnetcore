// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests
{
    public class RuleBuilderTest
    {
        [Fact]
        // see https://httpd.apache.org/docs/2.4/rewrite/advanced.html#setenvvars
        public void AddAction_Throws_ChangeEnvNotSupported()
        {
            var builder = new RuleBuilder();
            var flags = new Flags();
            flags.SetFlag(FlagType.Env, "rewritten:1");

            var ex = Assert.Throws<NotSupportedException>(() => builder.AddAction(null, flags));
            Assert.Equal(Resources.Error_ChangeEnvironmentNotSupported, ex.Message);
        }
    }
}