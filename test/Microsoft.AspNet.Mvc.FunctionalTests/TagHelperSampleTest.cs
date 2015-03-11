// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TagHelperSampleTest
    {
        private const string SiteName = nameof(TagHelperSample) + "." + nameof(TagHelperSample.Web);

        // Path relative to Mvc\\test\Microsoft.AspNet.Mvc.FunctionalTests
        private readonly static string SamplesFolder = Path.Combine("..", "..", "samples");

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

        private readonly ILoggerFactory _loggerFactory = new TestLoggerFactory();
        private readonly Action<IApplicationBuilder, ILoggerFactory> _app = new TagHelperSample.Web.Startup().Configure;

        [Fact]
        public async Task Home_Pages_ReturnSuccess()
        {
            // Arrange
            var server = TestHelper.CreateServer(app => _app(app, _loggerFactory), SiteName, SamplesFolder);
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

        private class TestLoggerFactory : ILoggerFactory
        {
            public LogLevel MinimumLevel { get; set; }

            public void AddProvider(ILoggerProvider provider)
            {

            }

            public ILogger CreateLogger(string name)
            {
                return new TestLogger();
            }
        }

        private class TestLogger : ILogger
        {
            public bool IsEnabled(LogLevel level)
            {
                return false;
            }

            public IDisposable BeginScope(object scope)
            {
                return new TestDisposable();
            }

            public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
            }
        }

        private class TestDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}