// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TagHelperSampleTest
    {
        private static readonly List<string> Paths = new List<string>
        {
            string.Empty,
            "/",
            "/Home/Create",
            "/Home/Create?Name=Billy&Blurb=hello&DateOfBirth=2000-11-30&YearsEmployeed=0",
            "/Home/Create",
            "/Home/Create?Name=Joe&Blurb=goodbye&DateOfBirth=1980-10-20&YearsEmployeed=1",
            "/Home/Edit/0",
            "/Home/Edit/0?Name=Bobby&Blurb=howdy&DateOfBirth=1999-11-30&YearsEmployeed=1",
            "/Home/Edit/1",
            "/Home/Edit/1?Name=Jack&Blurb=goodbye&DateOfBirth=1979-10-20&YearsEmployeed=4",
            "/Home/Edit/0",
            "/Home/Edit/0?Name=Bobby&Blurb=howdy&DateOfBirth=1999-11-30&YearsEmployeed=2",
            "/Home/Index",
        };

        // Path relative to Mvc\\test\Microsoft.AspNet.Mvc.FunctionalTests
        private readonly IServiceProvider _services =
            TestHelper.CreateServices("TagHelperSample.Web", Path.Combine("..", "..", "samples"));
        private readonly Action<IApplicationBuilder> _app = new TagHelperSample.Web.Startup().Configure;

        [Fact]
        public async Task Home_Pages_ReturnSuccess()
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();

                for (var index = 0; index < Paths.Count; index++)
                {
                    // Act
                    var path = Paths[index];
                    var response = await client.GetAsync("http://localhost" + path);

                    // Assert
                    Assert.NotNull(response);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
    }
}