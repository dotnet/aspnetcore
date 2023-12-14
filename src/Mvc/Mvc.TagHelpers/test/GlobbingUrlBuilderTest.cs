// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class GlobbingUrlBuilderTest
{
    [Fact]
    public void ReturnsOnlyStaticUrlWhenPatternDoesntFindAnyMatches()
    {
        // Arrange
        var fileProvider = MakeFileProvider();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestPathBase = PathString.Empty;
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList("/site.css", "**/*.css", excludePattern: null);

        // Assert
        Assert.Collection(urlList, url => Assert.Equal("/site.css", url));
    }

    [Fact]
    public void DedupesStaticUrlAndPatternMatches()
    {
        // Arrange
        var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestPathBase = PathString.Empty;
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList("/site.css", "**/*.css", excludePattern: null);

        // Assert
        Assert.Collection(urlList,
            url => Assert.Equal("/site.css", url),
            url => Assert.Equal("/blank.css", url));
    }

    public static TheoryData OrdersGlobbedMatchResultsCorrectly_Data
    {
        get
        {
            return new TheoryData<string, FileNode, string[]>
                {
                    {
                        /* staticUrl */ "/site.css",
                        /* dirStructure */ new FileNode(null, new [] {
                            new FileNode("B", new [] {
                                new FileNode("a.css"),
                                new FileNode("b.css"),
                                new FileNode("ba.css"),
                                new FileNode("b", new [] {
                                    new FileNode("a.css")
                                })
                            }),
                            new FileNode("A", new [] {
                                new FileNode("c.css"),
                                new FileNode("d.css")
                            }),
                            new FileNode("a.css")
                        }),
                        /* expectedPaths */ new []
                        {
                            "/site.css",
                            "/a.css",
                            "/A/c.css", "/A/d.css",
                            "/B/a.css", "/B/b.css", "/B/ba.css",
                            "/B/b/a.css"
                        }
                    },
                    {
                        /* staticUrl */ "/site.css",
                        /* dirStructure */ new FileNode(null, new [] {
                            new FileNode("A", new [] {
                                new FileNode("c.css"),
                                new FileNode("d.css")
                            }),
                            new FileNode("_A", new [] {
                                new FileNode("1.css"),
                                new FileNode("2.css")
                            }),
                            new FileNode("__A", new [] {
                                new FileNode("1.css"),
                                new FileNode("_1.css")
                            })
                        }),
                        /* expectedPaths */ new []
                        {
                            "/site.css",
                            "/A/c.css", "/A/d.css",
                            "/_A/1.css", "/_A/2.css",
                            "/__A/1.css", "/__A/_1.css"
                        }
                    },
                    {
                        /* staticUrl */ "/site.css",
                        /* dirStructure */ new FileNode(null, new [] {
                            new FileNode("A", new [] {
                                new FileNode("a.b.css"),
                                new FileNode("a-b.css"),
                                new FileNode("a_b.css"),
                                new FileNode("a.css"),
                                new FileNode("a.c.css")
                            })
                        }),
                        /* expectedPaths */ new []
                        {
                            "/site.css",
                            "/A/a.css", "/A/a-b.css", "/A/a.b.css", "/A/a.c.css", "/A/a_b.css"
                        }
                    },
                    {
                        /* staticUrl */ "/site.css",
                        /* dirStructure */ new FileNode(null, new [] {
                            new FileNode("B", new [] {
                                new FileNode("a.bss"),
                                new FileNode("a.css")
                            }),
                            new FileNode("A", new [] {
                                new FileNode("a.css"),
                                new FileNode("a.bss")
                            })
                        }),
                        /* expectedPaths */ new []
                        {
                            "/site.css",
                            "/A/a.bss", "/A/a.css",
                            "/B/a.bss", "/B/a.css"
                        }
                    },
                    {
                        /* staticUrl */ "/site.css",
                        /* dirStructure */ new FileNode(null, new [] {
                            new FileNode("B", new [] {
                                new FileNode("site2.css"),
                                new FileNode("site11.css")
                            }),
                            new FileNode("A", new [] {
                                new FileNode("site2.css"),
                                new FileNode("site11.css")
                            })
                        }),
                        /* expectedPaths */ new []
                        {
                            "/site.css",
                            "/A/site11.css", "/A/site2.css",
                            "/B/site11.css", "/B/site2.css"
                        }
                    },
                    {
                        /* staticUrl */ "/site.css",
                        /* dirStructure */ new FileNode(null, new [] {
                            new FileNode("B", new [] {
                                new FileNode("site"),
                                new FileNode("site.css")
                            }),
                            new FileNode("A", new [] {
                                new FileNode("site.css"),
                                new FileNode("site")
                            })
                        }),
                        /* expectedPaths */ new []
                        {
                            "/site.css",
                            "/A/site", "/A/site.css",
                            "/B/site", "/B/site.css"
                        }
                    },
                    {
                        /* staticUrl */ "/site.css",
                        /* dirStructure */ new FileNode(null, new [] {
                            new FileNode("B.B", new [] {
                                new FileNode("site"),
                                new FileNode("site.css")
                            }),
                            new FileNode("A.A", new [] {
                                new FileNode("site.css"),
                                new FileNode("site")
                            })
                        }),
                        /* expectedPaths */ new []
                        {
                            "/site.css",
                            "/A.A/site", "/A.A/site.css",
                            "/B.B/site", "/B.B/site.css"
                        }
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(OrdersGlobbedMatchResultsCorrectly_Data))]
    public void OrdersGlobbedMatchResultsCorrectly(string staticUrl, FileNode dirStructure, string[] expectedPaths)
    {
        // Arrange
        var fileProvider = MakeFileProvider(dirStructure);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestPathBase = PathString.Empty;
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList(staticUrl, "**/*.*", excludePattern: null);

        // Assert
        var collectionAssertions = expectedPaths.Select<string, Action<string>>(expected =>
            actual => Assert.Equal(expected, actual));
        Assert.Collection(urlList, collectionAssertions.ToArray());
    }

    [Theory]
    [InlineData("/sub")]
    [InlineData("/sub/again")]
    public void ResolvesMatchedUrlsAgainstPathBase(string pathBase)
    {
        // Arrange
        var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestPathBase = new PathString(pathBase);
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList(
            staticUrl: null,
            includePattern: "**/*.css",
            excludePattern: null);

        // Assert
        Assert.Collection(urlList,
            url => Assert.Equal($"{pathBase}/blank.css", url),
            url => Assert.Equal($"{pathBase}/site.css", url));
    }

    [Fact]
    public void UsesCachedMatchResults()
    {
        // Arrange
        var fileProvider = MakeFileProvider();
        var expected = new List<string> { "/blank.css", "/site.css" };
        var cache = MakeCache(result: expected);
        var requestPathBase = PathString.Empty;
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

        // Act
        var actual = globbingUrlBuilder.BuildUrlList(
            staticUrl: null,
            includePattern: "**/*.css",
            excludePattern: null);

        // Assert
        Assert.Collection(actual,
            url => Assert.Equal("/blank.css", url),
            url => Assert.Equal("/site.css", url));
    }

    [Fact]
    public void CachesMatchResults()
    {
        // Arrange
        var changeToken = new Mock<IChangeToken>();
        var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
        Mock.Get(fileProvider).Setup(f => f.Watch(It.IsAny<string>())).Returns(changeToken.Object);
        var value = new Mock<ICacheEntry>();
        value.Setup(c => c.Value).Returns(null);
        value.Setup(c => c.ExpirationTokens).Returns(new List<IChangeToken>());
        var cache = MakeCache();
        Mock.Get(cache).Setup(c => c.CreateEntry(
            /*key*/ It.IsAny<object>()))
            .Returns((object key) => value.Object)
            .Verifiable();
        var requestPathBase = PathString.Empty;
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList(
            staticUrl: null,
            includePattern: "**/*.css",
            excludePattern: null);

        // Assert
        Assert.Collection(urlList,
            url => Assert.Equal("/blank.css", url),
            url => Assert.Equal("/site.css", url));
        Mock.Get(cache).VerifyAll();
    }

    public static TheoryData CommaSeparatedPatternData
    {
        get
        {
            // Include pattern, expected output
            return new TheoryData<string, string[]>
                {
                    {
                        "~/*.css, ~/*.txt",
                        new[] { "/site.css", "/site2.txt" }
                    },
                    {
                        "*.css, /*.txt",
                        new[] { "/site.css", "/site2.txt" }
                    },
                    {
                        "\\*.css,~/*.txt",
                        new[] { "/site.css", "/site2.txt" }
                    },
                    {
                        "~/*.js, *.txt",
                        new[] { "/blank.js", "/site.js", "/site2.txt" }
                    },
                    {
                        " ~/*.js,*.txt, /*.css",
                        new[] { "/blank.js", "/site.css", "/site.js", "/site2.txt" }
                    },
                    {
                        "~/blank.js, blank.js,/blank.js, \\blank.js",
                        new[] { "/blank.js" }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(CommaSeparatedPatternData))]
    public void HandlesCommaSeparatedPatterns(string includePattern, string[] expectedOutput)
    {
        // Arrange
        var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.js", "site2.txt", "site.js"));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestPathBase = PathString.Empty;
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList(
            staticUrl: null,
            includePattern: includePattern,
            excludePattern: null);

        // Assert
        Assert.Equal(expectedOutput, urlList, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData(" \\")]
    [InlineData("~/")]
    [InlineData("  ~/")]
    public void TrimsLeadingTildeAndSlashFromPatterns(string prefix)
    {
        // Arrange
        var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestPathBase = PathString.Empty;
        var includePatterns = new List<string>();
        var excludePatterns = new List<string>();
        var matcher = MakeMatcher(includePatterns, excludePatterns);
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);
        globbingUrlBuilder.MatcherBuilder = () => matcher;

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList(
            staticUrl: null,
            includePattern: $"{prefix}**/*.css",
            excludePattern: $"{prefix}**/*.min.css");

        // Assert
        Assert.Collection(includePatterns, pattern => Assert.Equal("**/*.css", pattern));
        Assert.Collection(excludePatterns, pattern => Assert.Equal("**/*.min.css", pattern));
    }

    [Theory]
    [InlineData("~/")]
    [InlineData("/")]
    [InlineData("\\")]
    public void TrimsOnlySingleLeadingSlashOrTildeSlashFromPatterns(string prefix)
    {
        // Arrange
        var leadingSlashes = $"{prefix}{prefix}";
        var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var requestPathBase = PathString.Empty;
        var includePatterns = new List<string>();
        var excludePatterns = new List<string>();
        var matcher = MakeMatcher(includePatterns, excludePatterns);
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);
        globbingUrlBuilder.MatcherBuilder = () => matcher;

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList(
            staticUrl: null,
            includePattern: $"{leadingSlashes}**/*.css",
            excludePattern: $"{leadingSlashes}**/*.min.css");

        // Assert
        Assert.Collection(includePatterns, pattern => Assert.Equal($"{prefix}**/*.css", pattern));
        Assert.Collection(excludePatterns, pattern => Assert.Equal($"{prefix}**/*.min.css", pattern));
    }

    [Fact]
    public void BuildUrlList_AddsToMemoryCache_WithSizeLimit()
    {
        // Arrange
        var cacheEntry = Mock.Of<ICacheEntry>(m => m.ExpirationTokens == new List<IChangeToken>());
        var cache = Mock.Of<IMemoryCache>(m => m.CreateEntry(It.IsAny<object>()) == cacheEntry);

        var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
        var requestPathBase = PathString.Empty;
        var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

        // Act
        var urlList = globbingUrlBuilder.BuildUrlList("/site.css", "**/*.css", excludePattern: null);

        // Assert
        Assert.Equal(38, cacheEntry.Size);
    }

    public class FileNode
    {
        public FileNode(string name)
        {
            Name = name;
        }

        public FileNode(string name, IList<FileNode> children)
        {
            Name = name;
            Children = children;
        }

        public string Name { get; }

        public IList<FileNode> Children { get; }

        public bool IsDirectory => Children != null && Children.Any();
    }

    private static IFileInfo MakeFileInfo(string name, bool isDirectory = false)
    {
        var fileInfo = new Mock<IFileInfo>();
        fileInfo.Setup(f => f.Name).Returns(name);
        fileInfo.Setup(f => f.IsDirectory).Returns(isDirectory);
        return fileInfo.Object;
    }

    private static IFileProvider MakeFileProvider(FileNode rootNode)
    {
        if (rootNode.Children == null || !rootNode.Children.Any())
        {
            throw new ArgumentException($"{nameof(rootNode)} must have children.", nameof(rootNode));
        }

        var fileProvider = new Mock<IFileProvider>(MockBehavior.Strict);
        fileProvider.Setup(fp => fp.GetDirectoryContents(string.Empty))
            .Returns(MakeDirectoryContents(rootNode, fileProvider));
        fileProvider.Setup(fp => fp.Watch(It.IsAny<string>()))
            .Returns(new TestFileChangeToken());

        return fileProvider.Object;
    }

    private static IDirectoryContents MakeDirectoryContents(FileNode fileNode, Mock<IFileProvider> fileProviderMock)
    {
        var children = new List<IFileInfo>();

        foreach (var node in fileNode.Children)
        {
            children.Add(MakeFileInfo(node.Name, node.IsDirectory));
            if (node.IsDirectory)
            {
                var subPath = fileNode.Name != null
                    ? (fileNode.Name + "/" + node.Name)
                    : node.Name;
                fileProviderMock.Setup(fp => fp.GetDirectoryContents(subPath))
                    .Returns(MakeDirectoryContents(node, fileProviderMock));
            }
        }

        var directoryContents = new Mock<IDirectoryContents>();
        directoryContents.Setup(dc => dc.GetEnumerator()).Returns(children.GetEnumerator());

        return directoryContents.Object;
    }

    private static IDirectoryContents MakeDirectoryContents(params string[] fileNames)
    {
        var files = fileNames.Select(name => MakeFileInfo(name));
        var directoryContents = new Mock<IDirectoryContents>();
        directoryContents.Setup(dc => dc.GetEnumerator()).Returns(files.GetEnumerator());

        return directoryContents.Object;
    }

    private static IFileProvider MakeFileProvider(IDirectoryContents directoryContents = null)
    {
        if (directoryContents == null)
        {
            directoryContents = MakeDirectoryContents();
        }

        var fileProvider = new Mock<IFileProvider>();
        fileProvider.Setup(fp => fp.GetDirectoryContents(It.IsAny<string>()))
            .Returns(directoryContents);
        fileProvider.Setup(fp => fp.Watch(It.IsAny<string>()))
            .Returns(new TestFileChangeToken());
        return fileProvider.Object;
    }

    private static IMemoryCache MakeCache(object result = null)
    {
        var cache = new Mock<IMemoryCache>();
        cache.Setup(c => c.TryGetValue(It.IsAny<object>(), out result))
            .Returns(result != null);
        return cache.Object;
    }

    private static Matcher MakeMatcher(List<string> includePatterns, List<string> excludePatterns)
    {
        var matcher = new Mock<Matcher>();
        matcher.Setup(m => m.AddInclude(It.IsAny<string>()))
            .Returns<string>(pattern =>
            {
                includePatterns.Add(pattern);
                return matcher.Object;
            });
        matcher.Setup(m => m.AddExclude(It.IsAny<string>()))
            .Returns<string>(pattern =>
            {
                excludePatterns.Add(pattern);
                return matcher.Object;
            });
        var patternMatchingResult = new PatternMatchingResult(Enumerable.Empty<FilePatternMatch>());
        matcher.Setup(m => m.Execute(It.IsAny<DirectoryInfoBase>())).Returns(patternMatchingResult);
        return matcher.Object;
    }
}
