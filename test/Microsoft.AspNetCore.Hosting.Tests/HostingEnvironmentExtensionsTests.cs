// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class HostingEnvironmentExtensionsTests
    {
        [Fact]
        public void SetsFullPathToWwwroot()
        {
            var env = new HostingEnvironment();

            env.Initialize(".", new WebHostOptions() {WebRoot = "testroot"}, null);

            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.WebRootFileProvider);
        }

        [Fact(Skip = "Missing content publish property")]
        public void DefaultsToWwwrootSubdir()
        {
            var env = new HostingEnvironment();

            env.Initialize("testroot", new WebHostOptions(), null);

            Assert.Equal(Path.GetFullPath(Path.Combine("testroot","wwwroot")), env.WebRootPath);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.WebRootFileProvider);
        }

        [Fact]
        public void DefaultsToNullFileProvider()
        {
            var env = new HostingEnvironment();

            env.Initialize(Path.Combine("testroot", "wwwroot"), new WebHostOptions(), null);

            Assert.Null(env.WebRootPath);
            Assert.IsAssignableFrom<NullFileProvider>(env.WebRootFileProvider);
        }

        [Fact]
        public void SetsConfiguration()
        {
            var config = new ConfigurationBuilder().Build();
            var env = new HostingEnvironment();

            env.Initialize(".", new WebHostOptions(), config);

            Assert.Same(config, env.Configuration);
        }

        [Fact]
        public void OverridesEnvironmentFromConfig()
        {
            var env = new HostingEnvironment();
            env.EnvironmentName = "SomeName";

            env.Initialize(".", new WebHostOptions() { Environment = "NewName" }, null);

            Assert.Equal("NewName", env.EnvironmentName);
        }

        [Fact]
        public void MapPathThrowsWithNoWwwroot()
        {
            var env = new HostingEnvironment();

            env.Initialize(".", new WebHostOptions(), null);

            Assert.Throws<InvalidOperationException>(() => env.MapPath("file.txt"));
        }

    }
}
