// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TagHelperSampleTest : IClassFixture<MvcTestFixture<TagHelperSample.Web.Startup>>
    {
        public TagHelperSampleTest(MvcTestFixture<TagHelperSample.Web.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        public static TheoryData<string> PathData
        {
            get
            {
                var data = new TheoryData<string>
                {
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
                };

                // Three paths hit aspnet/External#50 with Mono on Mac.
                if (!TestPlatformHelper.IsMac || !TestPlatformHelper.IsMono)
                {
                    data.Add(string.Empty);
                    data.Add("/");
                    data.Add("/Home/Index");
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(PathData))]
        public async Task Home_Pages_ReturnSuccess(string path)
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost" + path);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}