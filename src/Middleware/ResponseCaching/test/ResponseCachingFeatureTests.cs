// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachingFeatureTests
    {
        public static TheoryData<string[]> ValidNullOrEmptyVaryRules
        {
            get
            {
                return new TheoryData<string[]>
                {
                    null,
                    new string[0],
                    new string[] { null },
                    new string[] { string.Empty }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValidNullOrEmptyVaryRules))]
        public void VaryByQueryKeys_Set_ValidEmptyValues_Succeeds(string[] value)
        {
            // Does not throw
            new ResponseCachingFeature().VaryByQueryKeys = value;
        }

        public static TheoryData<string[]> InvalidVaryRules
        {
            get
            {
                return new TheoryData<string[]>
                {
                    new string[] { null, null },
                    new string[] { null, string.Empty },
                    new string[] { string.Empty, null },
                    new string[] { string.Empty, "Valid" },
                    new string[] { "Valid", string.Empty },
                    new string[] { null, "Valid" },
                    new string[] { "Valid", null }
                };
            }
        }


        [Theory]
        [MemberData(nameof(InvalidVaryRules))]
        public void VaryByQueryKeys_Set_InValidEmptyValues_Throws(string[] value)
        {
            // Throws
            Assert.Throws<ArgumentException>(() => new ResponseCachingFeature().VaryByQueryKeys = value);
        }
    }
}
