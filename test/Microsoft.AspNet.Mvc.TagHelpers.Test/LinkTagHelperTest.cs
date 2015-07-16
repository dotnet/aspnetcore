// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Caching;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class LinkTagHelperTest
    {
        public static TheoryData MultiAttributeSameNameData
        {
            get
            {
                // outputAttributes
                return new TheoryData<TagHelperAttributeList>
                {
                    {
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" }
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" },
                            { "hello", "world3" }
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "HelLO", "world" },
                            { "HELLO", "world2" }
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "Hello", "world" },
                            { "HELLO", "world2" },
                            { "hello", "world3" }
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "HeLlO", "world" },
                            { "hello", "world2" }
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MultiAttributeSameNameData))]
        public void HandlesMultipleAttributesSameNameCorrectly(TagHelperAttributeList outputAttributes)
        {
            // Arrange
            var allAttributes = new TagHelperAttributeList(
                outputAttributes.Concat(
                    new TagHelperAttributeList
                    {
                        { "rel", new HtmlString("stylesheet") },
                        { "href", "test.css" },
                        { "asp-fallback-href", "test.css" },
                        { "asp-fallback-test-class", "hidden" },
                        { "asp-fallback-test-property", "visibility" },
                        { "asp-fallback-test-value", "hidden" },
                    }));
            var context = MakeTagHelperContext(allAttributes);
            var combinedOutputAttributes = new TagHelperAttributeList(
                outputAttributes.Concat(
                    new[]
                    {
                        new TagHelperAttribute("rel", new HtmlString("stylesheet"))
                    }));
            var output = MakeTagHelperOutput("link", combinedOutputAttributes);
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                ViewContext = viewContext,
                FallbackHref = "test.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visibility",
                FallbackTestValue = "hidden",
                Href = "test.css",
            };
            var expectedAttributes = new TagHelperAttributeList(output.Attributes);
            expectedAttributes.Add(new TagHelperAttribute("href", "test.css"));

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
        }

        public static TheoryData RunsWhenRequiredAttributesArePresent_Data
        {
            get
            {
                return new TheoryData<TagHelperAttributeList, Action<LinkTagHelper>>
                {
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-href"] = "test.css",
                            ["asp-fallback-test-class"] = "hidden",
                            ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackHref = "test.css";
                            tagHelper.FallbackTestClass = "hidden";
                            tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-href-include"] = "*.css",
                            ["asp-fallback-test-class"] = "hidden",
                            ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackHrefInclude = "*.css";
                            tagHelper.FallbackTestClass = "hidden";
                            tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                        }
                    },
                    // File Version
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-href"] = "test.css",
                            ["asp-fallback-test-class"] = "hidden",
                            ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackHref = "test.css";
                            tagHelper.FallbackTestClass = "hidden";
                            tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                            tagHelper.AppendVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-href-include"] = "*.css",
                            ["asp-fallback-test-class"] = "hidden",
                            ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackHrefInclude = "*.css";
                            tagHelper.FallbackTestClass = "hidden";
                            tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                            tagHelper.AppendVersion = true;
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RunsWhenRequiredAttributesArePresent_Data))]
        public void RunsWhenRequiredAttributesArePresent(
            TagHelperAttributeList attributes,
            Action<LinkTagHelper> setProperties)
        {
            // Arrange
            var context = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("link");
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { "/common.css" });

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                ViewContext = viewContext,
                GlobbingUrlBuilder = globbingUrlBuilder.Object
            };
            setProperties(helper);

            // Act
            helper.Process(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
            Assert.True(output.PostElement.IsModified);
        }

        public static TheoryData RunsWhenRequiredAttributesArePresent_NoHref_Data
        {
            get
            {
                return new TheoryData<TagHelperAttributeList, Action<LinkTagHelper>>
                {
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-href-include"] = "*.css"
                        },
                        tagHelper =>
                        {
                            tagHelper.HrefInclude = "*.css";
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-href-include"] = "*.css",
                            ["asp-href-exclude"] = "*.min.css"
                        },
                        tagHelper =>
                        {
                            tagHelper.HrefInclude = "*.css";
                            tagHelper.HrefExclude = "*.min.css";
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-href-include"] = "*.css",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.HrefInclude = "*.css";
                            tagHelper.AppendVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-href-include"] = "*.css",
                            ["asp-href-exclude"] = "*.min.css",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.HrefInclude = "*.css";
                            tagHelper.HrefExclude = "*.min.css";
                            tagHelper.AppendVersion = true;
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RunsWhenRequiredAttributesArePresent_NoHref_Data))]
        public void RunsWhenRequiredAttributesArePresent_NoHref(
            TagHelperAttributeList attributes,
            Action<LinkTagHelper> setProperties)
        {
            // Arrange
            var context = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("link");
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { "/common.css" });

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                ViewContext = viewContext,
                GlobbingUrlBuilder = globbingUrlBuilder.Object
            };
            setProperties(helper);

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Null(output.TagName);
            Assert.True(output.IsContentModified);
            Assert.True(output.PostElement.IsModified);
        }

        [Fact]
        public void PreservesOrderOfNonHrefAttributes()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", "test.css" },
                    { "data-extra", new HtmlString("something") },
                    { "asp-fallback-href", "test.css" },
                    { "asp-fallback-test-class", "hidden" },
                    { "asp-fallback-test-property", "visibility" },
                    { "asp-fallback-test-value", "hidden" },
                });
            var output = MakeTagHelperOutput("link",
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "data-extra", new HtmlString("something") },
                });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                ViewContext = viewContext,
                FallbackHref = "test.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visibility",
                FallbackTestValue = "hidden",
                Href = "test.css",
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("rel", output.Attributes[0].Name);
            Assert.Equal("data-extra", output.Attributes[1].Name);
            Assert.Equal("href", output.Attributes[2].Name);
        }

        public static TheoryData DoesNotRunWhenARequiredAttributeIsMissing_Data
        {
            get
            {
                return new TheoryData<TagHelperAttributeList, Action<LinkTagHelper>>
                {
                    {
                        new TagHelperAttributeList
                        {
                            // This is commented out on purpose: ["asp-href-include"] = "*.css",
                            ["asp-href-exclude"] = "*.min.css"
                        },
                        tagHelper =>
                        {
                            // This is commented out on purpose: tagHelper.HrefInclude = "*.css";
                            tagHelper.HrefExclude = "*.min.css";
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            // This is commented out on purpose: ["asp-fallback-href"] = "test.css",
                            ["asp-fallback-test-class"] = "hidden",
                            ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden"
                        },
                        tagHelper =>
                        {
                            // This is commented out on purpose: tagHelper.FallbackHref = "test.css";
                            tagHelper.FallbackTestClass = "hidden";
                            tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-href"] = "test.css",
                            ["asp-fallback-test-class"] = "hidden",
                            // This is commented out on purpose: ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackHref = "test.css";
                            tagHelper.FallbackTestClass = "hidden";
                            // This is commented out on purpose: tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            // This is commented out on purpose: ["asp-fallback-href-include"] = "test.css",
                            ["asp-fallback-href-exclude"] = "**/*.min.css",
                            ["asp-fallback-test-class"] = "hidden",
                            ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden"
                        },
                        tagHelper =>
                        {
                            // This is commented out on purpose: tagHelper.FallbackHrefInclude = "test.css";
                            tagHelper.FallbackHrefExclude = "**/*.min.css";
                            tagHelper.FallbackTestClass = "hidden";
                            tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(DoesNotRunWhenARequiredAttributeIsMissing_Data))]
        public void DoesNotRunWhenARequiredAttributeIsMissing(
            TagHelperAttributeList attributes,
            Action<LinkTagHelper> setProperties)
        {
            // Arrange
            var context = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("link");
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                ViewContext = viewContext,
            };
            setProperties(helper);

            // Act
            helper.Process(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
            Assert.Empty(output.Attributes);
            Assert.True(output.PostElement.IsEmpty);
        }

        [Fact]
        public void DoesNotRunWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var context = MakeTagHelperContext();
            var output = MakeTagHelperOutput("link");
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                ViewContext = viewContext,
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
            Assert.Empty(output.Attributes);
            Assert.True(output.PostElement.IsEmpty);
        }

        [Fact]
        public void RendersLinkTagsForGlobbedHrefResults()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", "/css/site.css" },
                    { "asp-href-include", "**/*.css" },
                });
            var output = MakeTagHelperOutput("link", attributes: new TagHelperAttributeList
            {
                { "rel", new HtmlString("stylesheet") },
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.css", null))
                .Returns(new[] { "/base.css" });

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                ViewContext = viewContext,
                Href = "/css/site.css",
                HrefInclude = "**/*.css",
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("link", output.TagName);
            Assert.Equal("/css/site.css", output.Attributes["href"].Value);
            Assert.Equal("<link rel=\"stylesheet\" href=\"HtmlEncode[[/base.css]]\" />", output.PostElement.GetContent());
        }

        [Fact]
        public void RendersLinkTagsForGlobbedHrefResults_UsingProvidedEncoder()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    ["rel"] = "stylesheet",
                    ["href"] = "/css/site.css",
                    ["asp-href-include"] = "**/*.css"
                });
            var output = MakeTagHelperOutput("link", attributes: new TagHelperAttributeList
            {
                ["rel"] = "stylesheet",
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.css", null))
                .Returns(new[] { "/base.css" });

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                ViewContext = viewContext,
                Href = "/css/site.css",
                HrefInclude = "**/*.css",
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("link", output.TagName);
            Assert.Equal("/css/site.css", output.Attributes["href"].Value);
            Assert.Equal("<link rel=\"HtmlEncode[[stylesheet]]\" href=\"HtmlEncode[[/base.css]]\" />",
                output.PostElement.GetContent());
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/21
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void RendersLinkTags_AddsFileVersion()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", "/css/site.css" },
                    { "asp-append-version", "true" }
                });
            var output = MakeTagHelperOutput("link", attributes: new TagHelperAttributeList
            {
                { "rel", new HtmlString("stylesheet") },
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                ViewContext = viewContext,
                Href = "/css/site.css",
                AppendVersion = true
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("link", output.TagName);
            Assert.Equal("/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["href"].Value);
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/21
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void RendersLinkTags_AddsFileVersion_WithRequestPathBase()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", "/bar/css/site.css" },
                    { "asp-append-version", "true" },
                });
            var output = MakeTagHelperOutput("link", attributes: new TagHelperAttributeList
            {
                { "rel", new HtmlString("stylesheet") },
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext("/bar");

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                ViewContext = viewContext,
                Href = "/bar/css/site.css",
                AppendVersion = true
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("link", output.TagName);
            Assert.Equal("/bar/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["href"].Value);
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/21
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void RendersLinkTags_GlobbedHref_AddsFileVersion()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", "/css/site.css" },
                    { "asp-href-include", "**/*.css" },
                    { "asp-append-version", "true" },
                });
            var output = MakeTagHelperOutput("link", attributes: new TagHelperAttributeList
            {
                { "rel", new HtmlString("stylesheet") },
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.css", null))
                .Returns(new[] { "/base.css" });

            var helper = new LinkTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new CommonTestEncoder(),
                new CommonTestEncoder())
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                ViewContext = viewContext,
                Href = "/css/site.css",
                HrefInclude = "**/*.css",
                AppendVersion = true
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("link", output.TagName);
            Assert.Equal("/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["href"].Value);
            Assert.Equal("<link rel=\"stylesheet\" href=\"HtmlEncode[[/base.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\" />",
                output.PostElement.GetContent());
        }

        private static ViewContext MakeViewContext(string requestPathBase = null)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            if (requestPathBase != null)
            {
                actionContext.HttpContext.Request.PathBase = new Http.PathString(requestPathBase);
            }

            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(metadataProvider);
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

            return viewContext;
        }

        private static TagHelperContext MakeTagHelperContext(
            TagHelperAttributeList attributes = null,
            string content = null)
        {
            attributes = attributes ?? new TagHelperAttributeList();

            return new TagHelperContext(
                attributes,
                items: new Dictionary<object, object>(),
                uniqueId: Guid.NewGuid().ToString("N"),
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent(content);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
        }

        private static TagHelperOutput MakeTagHelperOutput(string tagName, TagHelperAttributeList attributes = null)
        {
            attributes = attributes ?? new TagHelperAttributeList();

            return new TagHelperOutput(tagName, attributes);
        }

        private static IHostingEnvironment MakeHostingEnvironment()
        {
            var emptyDirectoryContents = new Mock<IDirectoryContents>();
            emptyDirectoryContents.Setup(dc => dc.GetEnumerator())
                .Returns(Enumerable.Empty<IFileInfo>().GetEnumerator());
            var mockFile = new Mock<IFileInfo>();
            mockFile.SetupGet(f => f.Exists).Returns(true);
            mockFile
                .Setup(m => m.CreateReadStream())
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("Hello World!")));
            var mockFileProvider = new Mock<IFileProvider>();
            mockFileProvider.Setup(fp => fp.GetDirectoryContents(It.IsAny<string>()))
                .Returns(emptyDirectoryContents.Object);
            mockFileProvider.Setup(fp => fp.GetFileInfo(It.IsAny<string>()))
                .Returns(mockFile.Object);
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.Setup(h => h.WebRootFileProvider).Returns(mockFileProvider.Object);

            return hostingEnvironment.Object;
        }

        private static IApplicationEnvironment MakeApplicationEnvironment(string applicationName = "testApplication")
        {
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns(applicationName);
            return applicationEnvironment.Object;
        }

        private static IMemoryCache MakeCache(object result = null)
        {
            var cache = new Mock<IMemoryCache>();
            cache.CallBase = true;
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out result))
                .Returns(result != null);

            cache
                .Setup(
                    c => c.Set(
                        /*key*/ It.IsAny<string>(),
                        /*value*/ It.IsAny<object>(),
                        /*options*/ It.IsAny<MemoryCacheEntryOptions>()))
                .Returns(result);
            return cache.Object;
        }
    }
}