// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class RequestCookiesCollectionTests
    {
        public static TheoryData UnEscapesKeyValues_Data
        {
            get
            {
                // key, value, expected
                return new TheoryData<string, string, string>
                {
                    { "key=value", "key", "value" },
                    { "key%2C=%21value", "key,", "!value" },
                    { "ke%23y%2C=val%5Eue", "ke#y,", "val^ue" },
                    { "base64=QUI%2BREU%2FRw%3D%3D", "base64", "QUI+REU/Rw==" },
                    { "base64=QUI+REU/Rw==", "base64", "QUI+REU/Rw==" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(UnEscapesKeyValues_Data))]
        public void UnEscapesKeyValues(
            string input,
            string expectedKey,
            string expectedValue)
        {
            var cookies = RequestCookieCollection.Parse(new StringValues(input));

            Assert.Equal(1, cookies.Count);
            Assert.Equal(expectedKey, cookies.Keys.Single());
            Assert.Equal(expectedValue, cookies[expectedKey]);
        }
    }
}
