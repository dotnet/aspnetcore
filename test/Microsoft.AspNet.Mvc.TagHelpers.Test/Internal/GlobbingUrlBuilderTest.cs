// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.Expiration.Interfaces;
using Microsoft.Framework.FileSystemGlobbing;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    public class GlobbingUrlBuilderTest
    {
        [Fact]
        public void ReturnsOnlyStaticUrlWhenPatternDoesntFindAnyMatches()
        {
            // Arrange
            var fileProvider = MakeFileProvider();
            IMemoryCache cache = null;
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
            IMemoryCache cache = null;
            var requestPathBase = PathString.Empty;
            var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

            // Act
            var urlList = globbingUrlBuilder.BuildUrlList("/site.css", "**/*.css", excludePattern: null);

            // Assert
            Assert.Collection(urlList,
                url => Assert.Equal("/site.css", url),
                url => Assert.Equal("/blank.css", url));
        }

        [Theory]
        [InlineData("/sub")]
        [InlineData("/sub/again")]
        public void ResolvesMatchedUrlsAgainstPathBase(string pathBase)
        {
            // Arrange
            var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
            IMemoryCache cache = null;
            var requestPathBase = new PathString(pathBase);
            var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

            // Act
            var urlList = globbingUrlBuilder.BuildUrlList(
                staticUrl: null,
                includePattern: "**/*.css",
                excludePattern: null);

            // Assert
            Assert.Collection(urlList,
                url => Assert.Equal($"{pathBase}/site.css", url),
                url => Assert.Equal($"{pathBase}/blank.css", url));
        }

        [Fact]
        public void UsesCachedMatchResults()
        {
            // Arrange
            var fileProvider = MakeFileProvider();
            var cache = MakeCache(new List<string> { "/site.css", "/blank.css" });
            var requestPathBase = PathString.Empty;
            var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);

            // Act
            var urlList = globbingUrlBuilder.BuildUrlList(
                staticUrl: null,
                includePattern: "**/*.css",
                excludePattern: null);

            // Assert
            Assert.Collection(urlList,
                url => Assert.Equal("/site.css", url),
                url => Assert.Equal("/blank.css", url));
        }

        [Fact]
        public void CachesMatchResults()
        {
            // Arrange
            var trigger = new Mock<IExpirationTrigger>();
            var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
            Mock.Get(fileProvider).Setup(f => f.Watch(It.IsAny<string>())).Returns(trigger.Object);
            var cache = MakeCache();
            var cacheSetContext = new Mock<ICacheSetContext>();
            cacheSetContext.Setup(c => c.AddExpirationTrigger(trigger.Object)).Verifiable();
            Mock.Get(cache).Setup(c => c.Set(
                /*key*/ It.IsAny<string>(),
                /*link*/ It.IsAny<IEntryLink>(),
                /*state*/ It.IsAny<object>(),
                /*create*/ It.IsAny<Func<ICacheSetContext, object>>()))
                .Returns<string, IEntryLink, object, Func<ICacheSetContext, object>>(
                    (key, link, state, create) =>
                    {
                        cacheSetContext.Setup(c => c.State).Returns(state);
                        return create(cacheSetContext.Object);
                    })
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
                url => Assert.Equal("/site.css", url),
                url => Assert.Equal("/blank.css", url));
            cacheSetContext.VerifyAll();
            Mock.Get(cache).VerifyAll();
        }

        [Theory]
        [InlineData("/")]
        [InlineData("\\")]
        public void TrimsLeadingSlashFromPatterns(string leadingSlash)
        {
            // Arrange
            var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
            IMemoryCache cache = null;
            var requestPathBase = PathString.Empty;
            var includePatterns = new List<string>();
            var excludePatterns = new List<string>();
            var matcher = MakeMatcher(includePatterns, excludePatterns);
            var globbingUrlBuilder = new GlobbingUrlBuilder(fileProvider, cache, requestPathBase);
            globbingUrlBuilder.MatcherBuilder = () => matcher;

            // Act
            var urlList = globbingUrlBuilder.BuildUrlList(
                staticUrl: null,
                includePattern: $"{leadingSlash}**/*.css",
                excludePattern: $"{leadingSlash}**/*.min.css");

            // Assert
            Assert.Collection(includePatterns, pattern => Assert.Equal("**/*.css", pattern));
            Assert.Collection(excludePatterns, pattern => Assert.Equal("**/*.min.css", pattern));
        }

        [Theory]
        [InlineData("/")]
        [InlineData("\\")]
        public void TrimsOnlySingleLeadingSlashFromPatterns(string leadingSlash)
        {
            // Arrange
            var leadingSlashes = $"{leadingSlash}{leadingSlash}";
            var fileProvider = MakeFileProvider(MakeDirectoryContents("site.css", "blank.css"));
            IMemoryCache cache = null;
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
            Assert.Collection(includePatterns, pattern => Assert.Equal($"{leadingSlash}**/*.css", pattern));
            Assert.Collection(excludePatterns, pattern => Assert.Equal($"{leadingSlash}**/*.min.css", pattern));
        }

        private static IFileInfo MakeFileInfo(string name)
        {
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(f => f.Name).Returns(name);
            return fileInfo.Object;
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
            return fileProvider.Object;
        }
        
        private static IMemoryCache MakeCache(object result = null)
        {
            var cache = new Mock<IMemoryCache>();
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), It.IsAny<IEntryLink>(), out result))
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
            var patternMatchingResult = new PatternMatchingResult(Enumerable.Empty<string>());
            matcher.Setup(m => m.Execute(It.IsAny<DirectoryInfoBase>())).Returns(patternMatchingResult);
            return matcher.Object;
        }
    }
}