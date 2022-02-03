// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.AspNetCore.StaticWebAssets;
using Microsoft.Extensions.FileProviders;
using Moq;

namespace Microsoft.AspNetCore.Hosting.Tests.StaticWebAssets;

public class ManifestStaticWebAssetsFileProviderTest
{
    [Fact]
    public void GetFileInfoPrefixRespectsOsCaseSensitivity()
    {
        // Arrange
        var comparer = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.PathComparer;
        var expectedResult = OperatingSystem.IsWindows();
        var manifest = new ManifestStaticWebAssetFileProvider.StaticWebAssetManifest();
        manifest.ContentRoots = new[] { Path.GetDirectoryName(typeof(ManifestStaticWebAssetsFileProviderTest).Assembly.Location) };
        manifest.Root = new()
        {
            Children = new(comparer)
            {
                ["_content"] = new()
                {
                    Children = new(comparer)
                    {
                        ["Microsoft.AspNetCore.Hosting.StaticWebAssets.xml"] = new()
                        {
                            Match = new()
                            {
                                ContentRoot = 0,
                                Path = "Microsoft.AspNetCore.Hosting.StaticWebAssets.xml"
                            }
                        }
                    }
                }
            }
        };

        var provider = new ManifestStaticWebAssetFileProvider(
            manifest,
            contentRoot => new PhysicalFileProvider(contentRoot));

        // Act
        var file = provider.GetFileInfo("/_CONTENT/Microsoft.AspNetCore.Hosting.StaticWebAssets.xml");

        // Assert
        Assert.Equal(expectedResult, file.Exists);
    }

    [Theory]
    [InlineData("/img/icon.png", true)]
    [InlineData("/Img/hero.gif", true)]
    // Note that we've changed the casing of the first segment
    [InlineData("/Img/icon.png", false)]
    [InlineData("/img/hero.gif", false)]
    public void ParseWorksWithNodesThatOnlyDifferOnCasing(string path, bool exists)
    {
        exists = exists | OperatingSystem.IsWindows();
        // Arrange
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        writer.Write(@"{
  ""ContentRoots"": [
    ""D:\\path\\"",
    ""D:\\other\\""
  ],
  ""Root"": {
    ""Children"": {
      ""img"": {
        ""Children"": {
          ""icon.png"": {
            ""Asset"": {
              ""ContentRootIndex"": 0,
              ""SubPath"": ""icon.png""
            }
          }
        }
      },
      ""Img"": {
        ""Children"": {
          ""hero.gif"": {
            ""Asset"": {
              ""ContentRootIndex"": 1,
              ""SubPath"": ""hero.gif""
            }
          }
        }
      }
    }
  }
}");
        var first = new Mock<IFileProvider>();
        first.Setup(s => s.GetFileInfo("icon.png")).Returns(new TestFileInfo() { Name = "icon.png", Exists = true });
        var second = new Mock<IFileProvider>();
        second.Setup(s => s.GetFileInfo("hero.gif")).Returns(new TestFileInfo() { Name = "hero.gif", Exists = true });

        writer.Flush();
        memoryStream.Seek(0, SeekOrigin.Begin);
        var manifest = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.Parse(memoryStream);
        var comparer = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.PathComparer;

        var provider = new ManifestStaticWebAssetFileProvider(
            manifest,
            contentRoot => contentRoot switch
            {
                "D:\\path\\" => first.Object,
                "D:\\other\\" => second.Object,
                _ => throw new InvalidOperationException("Unknown provider")
            });

        // Act
        var file = provider.GetFileInfo(path);

        // Assert
        Assert.Equal(exists, file.Exists);
    }

    [Theory]
    [InlineData("/img/Subdir/icon.png", true)]
    [InlineData("/Img/subdir/hero.gif", true)]
    // Note that we've changed the casing of the second segment
    [InlineData("/img/subdir/icon.png", false)]
    [InlineData("/Img/Subdir/hero.gif", false)]
    public void ParseWorksWithMergesNodesRecursively(string path, bool exists)
    {
        // Arrange
        exists = exists | OperatingSystem.IsWindows();
        var firstLevelCount = OperatingSystem.IsWindows() ? 1 : 2;
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        writer.Write(@"{
  ""ContentRoots"": [
    ""D:\\path\\"",
    ""D:\\other\\""
  ],
  ""Root"": {
    ""Children"": {
      ""img"": {
        ""Children"": {
          ""Subdir"": {
            ""Children"": {
              ""icon.png"": {
                ""Asset"": {
                  ""ContentRootIndex"": 0,
                  ""SubPath"": ""icon.png""
                }
              }
            }
          }
        }
      },
      ""Img"": {
        ""Children"": {
          ""subdir"": {
            ""Children"": {
              ""hero.gif"": {
                ""Asset"": {
                  ""ContentRootIndex"": 1,
                  ""SubPath"": ""hero.gif""
                }
              }
            }
          }
        }
      }
    }
  }
}");
        var first = new Mock<IFileProvider>();
        first.Setup(s => s.GetFileInfo("icon.png")).Returns(new TestFileInfo() { Name = "icon.png", Exists = true });
        var second = new Mock<IFileProvider>();
        second.Setup(s => s.GetFileInfo("hero.gif")).Returns(new TestFileInfo() { Name = "hero.gif", Exists = true });

        writer.Flush();
        memoryStream.Seek(0, SeekOrigin.Begin);
        var manifest = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.Parse(memoryStream);
        var comparer = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.PathComparer;

        var provider = new ManifestStaticWebAssetFileProvider(
            manifest,
            contentRoot => contentRoot switch
            {
                "D:\\path\\" => first.Object,
                "D:\\other\\" => second.Object,
                _ => throw new InvalidOperationException("Unknown provider")
            });

        // Act
        var file = provider.GetFileInfo(path);

        // Assert
        Assert.Equal(exists, file.Exists);
        Assert.Equal(firstLevelCount, manifest.Root.Children.Count);
        Assert.All(manifest.Root.Children.Values, c => Assert.Single(c.Children));
    }

    [Theory]
    [InlineData("/img/Subdir", true)]
    [InlineData("/Img/subdir/hero.gif", true)]
    // Note that we've changed the casing of the second segment
    [InlineData("/img/subdir", false)]
    [InlineData("/Img/Subdir/hero.gif", false)]
    public void ParseWorksFolderAndFileWithDiferentCasing(string path, bool exists)
    {
        // Arrange
        exists = exists | OperatingSystem.IsWindows();
        var firstLevelCount = OperatingSystem.IsWindows() ? 1 : 2;
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        // img/Subdir is a file without extension
        writer.Write(@"{
  ""ContentRoots"": [
    ""D:\\path\\"",
    ""D:\\other\\""
  ],
  ""Root"": {
    ""Children"": {
      ""img"": {
        ""Children"": {
          ""Subdir"": {
            ""Asset"": {
              ""ContentRootIndex"": 0,
              ""SubPath"": ""Subdir""
            }
          }
        }
      },
      ""Img"": {
        ""Children"": {
          ""subdir"": {
            ""Children"": {
              ""hero.gif"": {
                ""Asset"": {
                  ""ContentRootIndex"": 1,
                  ""SubPath"": ""hero.gif""
                }
              }
            }
          }
        }
      }
    }
  }
}");
        var first = new Mock<IFileProvider>();
        first.Setup(s => s.GetFileInfo("Subdir")).Returns(new TestFileInfo() { Name = "Subdir", Exists = true });
        var second = new Mock<IFileProvider>();
        second.Setup(s => s.GetFileInfo("hero.gif")).Returns(new TestFileInfo() { Name = "hero.gif", Exists = true });

        writer.Flush();
        memoryStream.Seek(0, SeekOrigin.Begin);
        var manifest = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.Parse(memoryStream);
        var comparer = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.PathComparer;

        var provider = new ManifestStaticWebAssetFileProvider(
            manifest,
            contentRoot => contentRoot switch
            {
                "D:\\path\\" => first.Object,
                "D:\\other\\" => second.Object,
                _ => throw new InvalidOperationException("Unknown provider")
            });

        // Act
        var file = provider.GetFileInfo(path);

        // Assert
        Assert.Equal(exists, file.Exists);
        Assert.Equal(firstLevelCount, manifest.Root.Children.Count);
        Assert.All(manifest.Root.Children.Values, c => Assert.Single(c.Children));
    }

    [Fact]
    public void CanFindFileListedOnTheManifest()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var file = fileProvider.GetFileInfo("_content/RazorClassLibrary/file.version.js");

        // Assert
        Assert.NotNull(file);
        Assert.True(file.Exists);
        Assert.Equal("file.version.js", file.Name);
    }

    [Fact]
    public void GetFileInfoHandlesRootCorrectly()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var file = fileProvider.GetFileInfo("");

        // Assert
        Assert.NotNull(file);
        Assert.False(file.Exists);
        Assert.False(file.IsDirectory);
        Assert.Equal("", file.Name);
    }

    [Fact]
    public void GetFileInfoHandlesRootPatternCorrectly()
    {
        var (manifest, factory) = CreateTestManifestWithPattern();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var file = fileProvider.GetFileInfo("/other.html");

        // Assert
        Assert.NotNull(file);
        Assert.True(file.Exists);
        Assert.False(file.IsDirectory);
        Assert.Equal("other.html", file.Name);
    }

    [Fact]
    public void GetDirectoryContentsHandlesRootPatternCorrectly()
    {
        var (manifest, factory) = CreateTestManifestWithPattern();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var contents = fileProvider.GetDirectoryContents("");

        // Assert
        Assert.NotNull(contents);
        Assert.True(contents.Exists);
        var file = contents.Single();
        Assert.True(file.Exists);
        Assert.False(file.IsDirectory);
        Assert.Equal("other.html", file.Name);
    }

    [Fact]
    public void CanFindFileMatchingPattern()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var file = fileProvider.GetFileInfo("_content/RazorClassLibrary/js/project-transitive-dep.js");

        // Assert
        Assert.NotNull(file);
        Assert.True(file.Exists);
        Assert.Equal("project-transitive-dep.js", file.Name);
    }

    [Fact]
    public void CanFindFileWithSpaces()
    {
        // Arrange
        var comparer = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.PathComparer;
        var expectedResult = OperatingSystem.IsWindows();
        var manifest = new ManifestStaticWebAssetFileProvider.StaticWebAssetManifest();
        manifest.ContentRoots = new[] { Path.Combine(AppContext.BaseDirectory, "testroot", "wwwroot") };
        manifest.Root = new()
        {
            Children = new(comparer)
            {
                ["_content"] = new()
                {
                    Children = new(comparer)
                    {
                        ["Static Web Assets.txt"] = new()
                        {
                            Match = new()
                            {
                                ContentRoot = 0,
                                Path = "Static Web Assets.txt"
                            }
                        }
                    }
                }
            }
        };

        var provider = new ManifestStaticWebAssetFileProvider(manifest, root => new PhysicalFileProvider(root));

        // Assert
        Assert.True(provider.GetFileInfo("/_content/Static Web Assets.txt").Exists);
    }

    [Fact]
    public void IgnoresFilesThatDontMatchThePattern()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var file = fileProvider.GetFileInfo("_content/RazorClassLibrary/styles.css");

        // Assert
        Assert.NotNull(file);
        Assert.False(file.Exists);
    }

    [Fact]
    public void ReturnsNotFoundFileWhenNoPatternAndNoEntryMatchPatch()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var file = fileProvider.GetFileInfo("_content/RazorClassLibrary/different");

        // Assert
        Assert.NotNull(file);
        Assert.False(file.IsDirectory);
        Assert.False(file.Exists);
    }

    [Fact]
    public void GetDirectoryContentsHandlesRootCorrectly()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var contents = fileProvider.GetDirectoryContents("");

        // Assert
        Assert.NotNull(contents);
        Assert.Equal(new[] { (true, "_content") }, contents.Select(e => (e.IsDirectory, e.Name)).OrderBy(e => e.Name).ToArray());
    }

    [Fact]
    public void GetDirectoryContentsReturnsNonExistingDirectoryWhenDirectoryDoesNotExist()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var contents = fileProvider.GetDirectoryContents("_content/NonExisting");

        // Assert
        Assert.NotNull(contents);
        Assert.False(contents.Exists);
    }

    [Fact]
    public void GetDirectoryContentsListsEntriesBasedOnManifest()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var contents = fileProvider.GetDirectoryContents("_content");

        // Assert
        Assert.NotNull(contents);
        Assert.Equal(new[]{
                (true, "AnotherClassLibrary"),
                (true, "RazorClassLibrary") },
            contents.Select(e => (e.IsDirectory, e.Name)).OrderBy(e => e.Name).ToArray());
    }

    [Fact]
    public void GetDirectoryContentsListsEntriesBasedOnPatterns()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var contents = fileProvider.GetDirectoryContents("_content/RazorClassLibrary/js");

        // Assert
        Assert.NotNull(contents);
        Assert.Equal(new[]{
                (false, "project-transitive-dep.js"),
                (false, "project-transitive-dep.v4.js") },
            contents.Select(e => (e.IsDirectory, e.Name)).OrderBy(e => e.Name).ToArray());
    }

    [Theory]
    [InlineData("\\", "_content")]
    [InlineData("\\_content\\RazorClassLib\\Dir", "Castle.Core.dll")]
    [InlineData("", "_content")]
    [InlineData("/", "_content")]
    [InlineData("/_content", "RazorClassLib")]
    [InlineData("/_content/RazorClassLib", "Dir")]
    [InlineData("/_content/RazorClassLib/Dir", "Microsoft.AspNetCore.Hosting.Tests.dll")]
    [InlineData("/_content/RazorClassLib/Dir/testroot/", "TextFile.txt")]
    [InlineData("/_content/RazorClassLib/Dir/testroot/wwwroot/", "README")]
    public void GetDirectoryContentsWalksUpContentRoot(string searchDir, string expected)
    {
        // Arrange
        var comparer = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.PathComparer;
        var expectedResult = OperatingSystem.IsWindows();
        var manifest = new ManifestStaticWebAssetFileProvider.StaticWebAssetManifest();
        manifest.ContentRoots = new[] { AppContext.BaseDirectory };
        manifest.Root = new()
        {
            Children = new(comparer)
            {
                ["_content"] = new()
                {
                    Children = new(comparer)
                    {
                        ["RazorClassLib"] = new()
                        {
                            Children = new(comparer)
                            {
                                ["Dir"] = new()
                                {
                                    Patterns = new ManifestStaticWebAssetFileProvider.StaticWebAssetPattern[]
                                    {
                                            new()
                                            {
                                                Pattern = "**",
                                                ContentRoot = 0,
                                                Depth = 3,
                                            }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var provider = new ManifestStaticWebAssetFileProvider(manifest, root => new PhysicalFileProvider(root));

        // Act
        var directory = provider.GetDirectoryContents(searchDir);

        // Assert
        Assert.NotEmpty(directory);
        Assert.Contains(directory, file => string.Equals(file.Name, expected));
    }

    [Theory]
    [InlineData("/_content/RazorClass")]
    public void GetDirectoryContents_PartialMatchFails(string requestedUrl)
    {
        // Arrange
        var comparer = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.PathComparer;
        var expectedResult = OperatingSystem.IsWindows();
        var manifest = new ManifestStaticWebAssetFileProvider.StaticWebAssetManifest();
        manifest.ContentRoots = new[] { AppContext.BaseDirectory };
        manifest.Root = new()
        {
            Children = new(comparer)
            {
                ["_content"] = new()
                {
                    Children = new(comparer)
                    {
                        ["RazorClassLib"] = new()
                        {
                            Children = new(comparer)
                            {
                                ["Dir"] = new()
                                {
                                }
                            }
                        }
                    }
                }
            }
        };

        var provider = new ManifestStaticWebAssetFileProvider(manifest, root => new PhysicalFileProvider(root));

        // Act
        var directory = provider.GetDirectoryContents(requestedUrl);

        // Assert
        Assert.Empty(directory);
    }

    [Fact]
    public void CombinesContentsFromManifestAndPatterns()
    {
        var (manifest, factory) = CreateTestManifest();

        var fileProvider = new ManifestStaticWebAssetFileProvider(manifest, factory);

        // Act
        var contents = fileProvider.GetDirectoryContents("_content/RazorClassLibrary");

        // Assert
        Assert.NotNull(contents);
        Assert.Equal(new[]{
                (false, "file.version.js"),
                (true, "js") },
            contents.Select(e => (e.IsDirectory, e.Name)).OrderBy(e => e.Name).ToArray());
    }

    [Fact]
    public void GetDirectoryContentsPrefixRespectsOsCaseSensitivity()
    {
        // Arrange
        var comparer = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.PathComparer;
        var expectedResult = OperatingSystem.IsWindows();
        var manifest = new ManifestStaticWebAssetFileProvider.StaticWebAssetManifest();
        manifest.ContentRoots = new[] { Path.GetDirectoryName(typeof(ManifestStaticWebAssetsFileProviderTest).Assembly.Location) };
        manifest.Root = new()
        {
            Children = new(comparer)
            {
                ["_content"] = new()
                {
                    Children = new(comparer)
                    {
                        ["Microsoft.AspNetCore.Hosting.StaticWebAssets.xml"] = new()
                        {
                            Match = new()
                            {
                                ContentRoot = 0,
                                Path = "Microsoft.AspNetCore.Hosting.StaticWebAssets.xml"
                            }
                        }
                    }
                }
            }
        };

        var provider = new ManifestStaticWebAssetFileProvider(
            manifest,
            contentRoot => new PhysicalFileProvider(contentRoot));

        // Act
        var directory = provider.GetDirectoryContents("/_CONTENT/");

        // Assert
        Assert.Equal(expectedResult, directory.Exists);
    }

    private static (ManifestStaticWebAssetFileProvider.StaticWebAssetManifest manifest, Func<string, IFileProvider> factory) CreateTestManifest()
    {
        // Arrange
        var manifest = new ManifestStaticWebAssetFileProvider.StaticWebAssetManifest();
        manifest.ContentRoots = new string[2] {
                "Cero",
                "Uno"
            };
        Func<string, IFileProvider> factory = (string contentRoot) =>
        {
            if (contentRoot == "Cero")
            {
                var styles = new TestFileInfo { Exists = true, IsDirectory = false, Name = "styles.css" };
                var js = new TestFileInfo { Exists = true, IsDirectory = true, Name = "js" };
                var file = new TestFileInfo { Exists = true, Name = "file.js", IsDirectory = false };
                var transitiveDep = new TestFileInfo { Exists = true, IsDirectory = false, Name = "project-transitive-dep.js" };
                var transitiveDepV4 = new TestFileInfo { Exists = true, IsDirectory = false, Name = "project-transitive-dep.v4.js" };
                var providerMock = new Mock<IFileProvider>();
                providerMock.Setup(p => p.GetDirectoryContents("")).Returns(new TestDirectoryContents(new[] { styles, js }));
                providerMock.Setup(p => p.GetDirectoryContents("js")).Returns(new TestDirectoryContents(new[]
                {
                        transitiveDep,
                        transitiveDepV4
                }));
                providerMock.Setup(p => p.GetFileInfo("different")).Returns(new NotFoundFileInfo("different"));
                providerMock.Setup(p => p.GetFileInfo("file.js")).Returns(file);
                providerMock.Setup(p => p.GetFileInfo("js")).Returns(js);
                providerMock.Setup(p => p.GetFileInfo("styles.css")).Returns(styles);
                providerMock.Setup(p => p.GetFileInfo("js/project-transitive-dep.js")).Returns(transitiveDep);
                providerMock.Setup(p => p.GetFileInfo("js/project-transitive-dep.v4.js")).Returns(transitiveDepV4);

                return providerMock.Object;
            }
            if (contentRoot == "Uno")
            {
                var css = new TestFileInfo { Exists = true, IsDirectory = true, Name = "css" };
                var site = new TestFileInfo { Exists = true, IsDirectory = false, Name = "site.css" };
                var js = new TestFileInfo { Exists = true, IsDirectory = true, Name = "js" };
                var projectDirectDep = new TestFileInfo { Exists = true, IsDirectory = false, Name = "project-direct-dep.js" };
                var providerMock = new Mock<IFileProvider>();
                providerMock.Setup(p => p.GetDirectoryContents("")).Returns(new TestDirectoryContents(new[] { css, js }));
                providerMock.Setup(p => p.GetDirectoryContents("js")).Returns(new TestDirectoryContents(new[]
                {
                        projectDirectDep
                }));
                providerMock.Setup(p => p.GetDirectoryContents("css")).Returns(new TestDirectoryContents(new[]
                {
                        site
                }));

                providerMock.Setup(p => p.GetFileInfo("js")).Returns(js);
                providerMock.Setup(p => p.GetFileInfo("css")).Returns(css);
                providerMock.Setup(p => p.GetFileInfo("css/site.css")).Returns(site);
                providerMock.Setup(p => p.GetFileInfo("js/project-direct-dep.js")).Returns(projectDirectDep);

                return providerMock.Object;
            }

            throw new InvalidOperationException("Invalid content root");
        };
        manifest.Root = new()
        {
            Children = new()
            {
                ["_content"] = new()
                {
                    Children = new()
                    {
                        ["RazorClassLibrary"] = new()
                        {
                            Children = new() { ["file.version.js"] = new() { Match = new() { ContentRoot = 0, Path = "file.js" } } },
                            Patterns = new ManifestStaticWebAssetFileProvider.StaticWebAssetPattern[] { new() { ContentRoot = 0, Depth = 2, Pattern = "**/*.js" } },
                        },
                        ["AnotherClassLibrary"] = new()
                        {
                            Patterns = new ManifestStaticWebAssetFileProvider.StaticWebAssetPattern[] { new() { ContentRoot = 1, Depth = 2, Pattern = "**" } }
                        }
                    }
                }
            }
        };

        return (manifest, factory);
    }

    private static (ManifestStaticWebAssetFileProvider.StaticWebAssetManifest manifest, Func<string, IFileProvider> factory) CreateTestManifestWithPattern()
    {
        // Arrange
        var otherHtml = new TestFileInfo { Exists = true, IsDirectory = false, Name = "other.html" };

        var manifest = new ManifestStaticWebAssetFileProvider.StaticWebAssetManifest();
        manifest.ContentRoots = new[] { "Cero" };
        Func<string, IFileProvider> factory = (string contentRoot) =>
        {
            if (contentRoot == "Cero")
            {
                var providerMock = new Mock<IFileProvider>();
                providerMock.Setup(p => p.GetFileInfo("other.html")).Returns(otherHtml);
                providerMock.Setup(p => p.GetDirectoryContents("")).Returns(new TestDirectoryContents(new[] { otherHtml })
                {
                    Exists = true
                });

                return providerMock.Object;
            }

            throw new InvalidOperationException("Invalid content root");
        };
        manifest.Root = new()
        {
            Children = new Dictionary<string, ManifestStaticWebAssetFileProvider.StaticWebAssetNode>(),
            Patterns = new[]
            {
                    new ManifestStaticWebAssetFileProvider.StaticWebAssetPattern
                    {
                        ContentRoot = 0,
                        Depth = 0,
                        Pattern = "**"
                    }
                }
        };

        return (manifest, factory);
    }
}

internal class TestFileInfo : IFileInfo
{
    public bool Exists { get; set; }

    public long Length { get; set; }

    public string PhysicalPath { get; set; }

    public string Name { get; set; }

    public DateTimeOffset LastModified { get; set; }

    public bool IsDirectory { get; set; }

    public Stream CreateReadStream() => Stream.Null;
}

internal class TestDirectoryContents : IDirectoryContents
{
    private readonly IEnumerable<IFileInfo> _contents;

    public TestDirectoryContents(IEnumerable<IFileInfo> contents)
    {
        _contents = contents;
    }

    public bool Exists { get; set; }

    public IEnumerator<IFileInfo> GetEnumerator() => _contents.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
