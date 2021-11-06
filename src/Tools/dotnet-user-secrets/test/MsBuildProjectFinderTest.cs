// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Xunit;

namespace Microsoft.Extensions.SecretManager.Tools.Tests;

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

            Assert.Equal(Path.Combine(files.Root, filename), finder.FindMsBuildProject(null));
        }
    }

    [Fact]
    public void ThrowsWhenNoFile()
    {
        using (var files = new TemporaryFileProvider())
        {
            var finder = new MsBuildProjectFinder(files.Root);

            Assert.Throws<FileNotFoundException>(() => finder.FindMsBuildProject(null));
        }
    }

    [Fact]
    public void DoesNotMatchXproj()
    {
        using (var files = new TemporaryFileProvider())
        {
            var finder = new MsBuildProjectFinder(files.Root);
            files.Add("test.xproj", "");

            Assert.Throws<FileNotFoundException>(() => finder.FindMsBuildProject(null));
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

            Assert.Throws<FileNotFoundException>(() => finder.FindMsBuildProject(null));
        }
    }

    [Fact]
    public void ThrowsWhenFileDoesNotExist()
    {
        using (var files = new TemporaryFileProvider())
        {
            var finder = new MsBuildProjectFinder(files.Root);

            Assert.Throws<FileNotFoundException>(() => finder.FindMsBuildProject("test.csproj"));
        }
    }

    [Fact]
    public void ThrowsWhenRootDoesNotExist()
    {
        var files = new TemporaryFileProvider();
        var finder = new MsBuildProjectFinder(files.Root);
        files.Dispose();
        Assert.Throws<FileNotFoundException>(() => finder.FindMsBuildProject(null));
    }
}
