// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class HostingEnvironmentExtensionsTests
    {
        [Fact]
        public void SetsFullPathToWwwroot()
        {
            IWebHostEnvironment env = new HostingEnvironment();

            var webHostOptions = CreateWebHostOptions();
            webHostOptions.WebRoot = "testroot";

            env.Initialize(Path.GetFullPath("."), webHostOptions);

            Assert.Equal(Path.GetFullPath("."), env.ContentRootPath);
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.ContentRootFileProvider);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.WebRootFileProvider);
        }

        [Fact]
        public void DefaultsToWwwrootSubdir()
        {
            IWebHostEnvironment env = new HostingEnvironment();

            env.Initialize(Path.GetFullPath("testroot"), CreateWebHostOptions());

            Assert.Equal(Path.GetFullPath("testroot"), env.ContentRootPath);
            Assert.Equal(Path.GetFullPath(Path.Combine("testroot", "wwwroot")), env.WebRootPath);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.ContentRootFileProvider);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.WebRootFileProvider);
        }

        [Fact]
        public void DefaultsToNullFileProvider()
        {
            IWebHostEnvironment env = new HostingEnvironment();

            env.Initialize(Path.GetFullPath(Path.Combine("testroot", "wwwroot")), CreateWebHostOptions());

            Assert.Equal(Path.GetFullPath(Path.Combine("testroot", "wwwroot")), env.ContentRootPath);
            Assert.Null(env.WebRootPath);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.ContentRootFileProvider);
            Assert.IsAssignableFrom<NullFileProvider>(env.WebRootFileProvider);
        }

        [Fact]
        public void OverridesEnvironmentFromConfig()
        {
            IWebHostEnvironment env = new HostingEnvironment();
            env.EnvironmentName = "SomeName";

            var webHostOptions = CreateWebHostOptions();
            webHostOptions.Environment = "NewName";

            env.Initialize(Path.GetFullPath("."), webHostOptions);

            Assert.Equal("NewName", env.EnvironmentName);
        }

        private WebHostOptions CreateWebHostOptions(IConfiguration configuration = null, string applicationNameFallback = null)
        {
            return new WebHostOptions(
                configuration ?? Mock.Of<IConfiguration>(),
                applicationNameFallback: applicationNameFallback);
        }
    }
}
