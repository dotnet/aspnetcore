// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.ProjectModel.MsBuild
{
    public class MsBuildProjectFinderTest
    {
        [Theory]
        [InlineData(".csproj")]
        [InlineData(".vbproj")]
        [InlineData(".fsproj")]
        public void FindsSingleProject(string extension)
        {
            using (var files = new TemporaryFileProvider())
            {
                var filename = "TestProject" + extension;
                files.Add(filename, "");

                var finder = new MsBuildProjectFinder(files.Root);

                Assert.Equal(Path.Combine(files.Root, filename), finder.FindMsBuildProject());
            }
        }

        [Fact]
        public void ThrowsWhenNoFile()
        {
            using (var files = new TemporaryFileProvider())
            {
                var finder = new MsBuildProjectFinder(files.Root);

                Assert.Throws<InvalidOperationException>(() => finder.FindMsBuildProject());
            }
        }

        [Fact]
        public void ThrowsWhenMultipleFile()
        {
            using (var files = new TemporaryFileProvider())
            {
                files.Add("Test1.csproj", "");
                files.Add("Test2.csproj", "");
                var finder = new MsBuildProjectFinder(files.Root);

                Assert.Throws<InvalidOperationException>(() => finder.FindMsBuildProject());
            }
        }

        [Fact]
        public void ThrowsWhenFileDoesNotExist()
        {
            using (var files = new TemporaryFileProvider())
            {
                var finder = new MsBuildProjectFinder(files.Root);

                Assert.Throws<InvalidOperationException>(() => finder.FindMsBuildProject("test.csproj"));
            }
        }

        [Fact]
        public void ThrowsWhenRootDoesNotExist()
        {
            var files = new TemporaryFileProvider();
            var finder = new MsBuildProjectFinder(files.Root);
            files.Dispose();
            Assert.Throws<InvalidOperationException>(() => finder.FindMsBuildProject());
        }
    }
}
