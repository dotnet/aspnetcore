// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class IdentityResultTest
    {
        [Fact]
        public void VerifyDefaultConstructor()
        {
            var result = new IdentityResult();
            Assert.False(result.Succeeded);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void NullFailedUsesEmptyErrors()
        {
            var result = IdentityResult.Failed();
            Assert.False(result.Succeeded);
            Assert.Empty(result.Errors);
        }
    }
}
