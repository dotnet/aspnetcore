// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class HttpMethodProviderAttributesTests
    {
        [Theory]
        [MemberData(nameof(HttpMethodProviderTestData))]
        public void HttpMethodProviderAttributes_ReturnsCorrectHttpMethodSequence(
            IActionHttpMethodProvider httpMethodProvider,
            IEnumerable<string> expectedHttpMethods)
        {
            // Act & Assert
            Assert.Equal(expectedHttpMethods, httpMethodProvider.HttpMethods);
        }

        public static TheoryData<IActionHttpMethodProvider, IEnumerable<string>> HttpMethodProviderTestData
        {
            get
            {
                var data = new TheoryData<IActionHttpMethodProvider, IEnumerable<string>>();
                data.Add(new HttpGetAttribute(), new[] { "GET" });
                data.Add(new HttpPostAttribute(), new[] { "POST" });
                data.Add(new HttpPutAttribute(), new[] { "PUT" });
                data.Add(new HttpPatchAttribute(), new[] { "PATCH" });
                data.Add(new HttpDeleteAttribute(), new[] { "DELETE" });
                data.Add(new HttpHeadAttribute(), new[] { "HEAD" });
                data.Add(new HttpOptionsAttribute(), new[] { "OPTIONS" });
                data.Add(new AcceptVerbsAttribute("MERGE", "OPTIONS"), new[] { "MERGE", "OPTIONS" });

                return data;
            }
        }
    }
}