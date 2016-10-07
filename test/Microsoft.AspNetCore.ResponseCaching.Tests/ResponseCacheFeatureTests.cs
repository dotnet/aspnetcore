// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCacheFeatureTests
    {
        public static TheoryData<StringValues> ValidNullOrEmptyVaryRules
        {
            get
            {
                return new TheoryData<StringValues>
                {
                    default(StringValues),
                    StringValues.Empty,
                    new StringValues((string)null),
                    new StringValues(string.Empty),
                    new StringValues((string[])null),
                    new StringValues(new string[0]),
                    new StringValues(new string[] { null }),
                    new StringValues(new string[] { string.Empty })
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValidNullOrEmptyVaryRules))]
        public void VaryByQueryKeys_Set_ValidEmptyValues_Succeeds(StringValues value)
        {
            // Does not throw
            new ResponseCacheFeature().VaryByQueryKeys = value;
        }

        public static TheoryData<StringValues> InvalidVaryRules
        {
            get
            {
                return new TheoryData<StringValues>
                {
                    new StringValues(new string[] { null, null }),
                    new StringValues(new string[] { null, string.Empty }),
                    new StringValues(new string[] { string.Empty, null }),
                    new StringValues(new string[] { string.Empty, "Valid" }),
                    new StringValues(new string[] { "Valid", string.Empty }),
                    new StringValues(new string[] { null, "Valid" }),
                    new StringValues(new string[] { "Valid", null })
                };
            }
        }


        [Theory]
        [MemberData(nameof(InvalidVaryRules))]
        public void VaryByQueryKeys_Set_InValidEmptyValues_Throws(StringValues value)
        {
            // Throws
            Assert.Throws<ArgumentException>(() => new ResponseCacheFeature().VaryByQueryKeys = value);
        }
    }
}
