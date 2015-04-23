// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Expiration.Interfaces;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.WebEncoders;
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
                // outputAttributes, expectedAttributeString
                return new TheoryData<TagHelperAttributeList, string>
                {
                    {
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" }
                        },
                        "hello=\"HtmlEncode[[world]]\" hello=\"HtmlEncode[[world2]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" },
                            { "hello", "world3" }
                        },
                        "hello=\"HtmlEncode[[world]]\" hello=\"HtmlEncode[[world2]]\" hello=\"HtmlEncode[[world3]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "HelLO", "world" },
                            { "HELLO", "world2" }
                        },
                        "HelLO=\"HtmlEncode[[world]]\" HELLO=\"HtmlEncode[[world2]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "Hello", "world" },
                            { "HELLO", "world2" },
                            { "hello", "world3" }
                        },
                        "Hello=\"HtmlEncode[[world]]\" HELLO=\"HtmlEncode[[world2]]\" hello=\"HtmlEncode[[world3]]\""
                    },
                    {
                        new TagHelperAttributeList
                        {
                            { "HeLlO", "world" },
                            { "hello", "world2" }
                        },
                        "HeLlO=\"HtmlEncode[[world]]\" hello=\"HtmlEncode[[world2]]\""
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MultiAttributeSameNameData))]
        public void HandlesMultipleAttributesSameNameCorrectly(
            TagHelperAttributeList outputAttributes,
            string expectedAttributeString)
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
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new CommonTestEncoder(),
                JavaScriptEncoder = new CommonTestEncoder(),
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                FallbackHref = "test.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visibility",
                FallbackTestValue = "hidden",
                Href = "test.css",
                Cache = MakeCache(),
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.StartsWith("<link " + expectedAttributeString + " rel=\"stylesheet\"", output.Content.GetContent());
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
                            ["asp-file-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FileVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-href-include"] = "*.css",
                            ["asp-file-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.HrefInclude = "*.css";
                            tagHelper.FileVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-href-include"] = "*.css",
                            ["asp-href-exclude"] = "*.min.css",
                            ["asp-file-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.HrefInclude = "*.css";
                            tagHelper.HrefExclude = "*.min.css";
                            tagHelper.FileVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-href"] = "test.css",
                            ["asp-fallback-test-class"] = "hidden",
                            ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden",
                            ["asp-file-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackHref = "test.css";
                            tagHelper.FallbackTestClass = "hidden";
                            tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                            tagHelper.FileVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-href-include"] = "*.css",
                            ["asp-fallback-test-class"] = "hidden",
                            ["asp-fallback-test-property"] = "visibility",
                            ["asp-fallback-test-value"] = "hidden",
                            ["asp-file-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackHrefInclude = "*.css";
                            tagHelper.FallbackTestClass = "hidden";
                            tagHelper.FallbackTestProperty = "visibility";
                            tagHelper.FallbackTestValue = "hidden";
                            tagHelper.FileVersion = true;
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
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new CommonTestEncoder(),
                JavaScriptEncoder = new CommonTestEncoder(),
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                Cache = MakeCache()
            };
            setProperties(helper);

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Null(output.TagName);
            Assert.NotNull(output.Content);
            Assert.True(output.IsContentModified);
        }

        [Fact]
        public void PreservesOrderOfSourceAttributesWhenRun()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "data-extra", new HtmlString("something") },
                    { "href", "test.css" },
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
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new CommonTestEncoder(),
                JavaScriptEncoder = new CommonTestEncoder(),
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                FallbackHref = "test.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visibility",
                FallbackTestValue = "hidden",
                Href = "test.css",
                Cache = MakeCache(),
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.StartsWith(
                "<link rel=\"stylesheet\" data-extra=\"something\" href=\"HtmlEncode[[test.css]]\"", output.Content.GetContent());
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
            var helper = new LinkTagHelper
            {
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                Cache = MakeCache(),
            };
            setProperties(helper);

            // Act
            helper.Process(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
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
            var helper = new LinkTagHelper
            {
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                Cache = MakeCache(),
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
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
            globbingUrlBuilder.Setup(g => g.BuildUrlList("/css/site.css", "**/*.css", null))
                .Returns(new[] { "/css/site.css", "/base.css" });
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new CommonTestEncoder(),
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                Href = "/css/site.css",
                HrefInclude = "**/*.css",
                Cache = MakeCache(),
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(
                "<link rel=\"stylesheet\" href=\"HtmlEncode[[/css/site.css]]\" />" +
                "<link rel=\"stylesheet\" href=\"HtmlEncode[[/base.css]]\" />",
                output.Content.GetContent());
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
            globbingUrlBuilder.Setup(g => g.BuildUrlList("/css/site.css", "**/*.css", null))
                .Returns(new[] { "/css/site.css", "/base.css" });
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new CommonTestEncoder(),
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                Href = "/css/site.css",
                HrefInclude = "**/*.css",
                Cache = MakeCache(),
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(
                "<link rel=\"HtmlEncode[[stylesheet]]\" href=\"HtmlEncode[[/css/site.css]]\" />" +
                "<link rel=\"HtmlEncode[[stylesheet]]\" href=\"HtmlEncode[[/base.css]]\" />",
                output.Content.GetContent());
        }

        [Fact]
        public void RendersLinkTags_AddsFileVersion()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", "/css/site.css" },
                    { "asp-file-version", "true" }
                });
            var output = MakeTagHelperOutput("link", attributes: new TagHelperAttributeList
            {
                { "rel", new HtmlString("stylesheet") },
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new CommonTestEncoder(),
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                Href = "/css/site.css",
                HrefInclude = "**/*.css",
                FileVersion = true,
                Cache = MakeCache(),
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(
                "<link rel=\"stylesheet\" href=\"HtmlEncode[[/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\" />",
                output.Content.GetContent());
        }

        [Fact]
        public void RendersLinkTags_AddsFileVersion_WithRequestPathBase()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", "/bar/css/site.css" },
                    { "asp-file-version", "true" },
                });
            var output = MakeTagHelperOutput("link", attributes: new TagHelperAttributeList
            {
                { "rel", new HtmlString("stylesheet") },
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext("/bar");
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new CommonTestEncoder(),
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                Href = "/bar/css/site.css",
                HrefInclude = "**/*.css",
                FileVersion = true,
                Cache = MakeCache(),
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(
                "<link rel=\"stylesheet\" href=\"HtmlEncode[[/bar/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\" />",
                output.Content.GetContent());
        }

        [Fact]
        public void RendersLinkTags_GlobbedHref_AddsFileVersion()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", "/css/site.css" },
                    { "asp-href-include", "**/*.css" },
                    { "asp-file-version", "true" },
                });
            var output = MakeTagHelperOutput("link", attributes: new TagHelperAttributeList
            {
                { "rel", new HtmlString("stylesheet") },
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList("/css/site.css", "**/*.css", null))
                .Returns(new[] { "/css/site.css", "/base.css" });
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new CommonTestEncoder(),
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                Href = "/css/site.css",
                HrefInclude = "**/*.css",
                FileVersion = true,
                Cache = MakeCache(),
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(
                "<link rel=\"stylesheet\" href=\"HtmlEncode[[/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\" />" +
                "<link rel=\"stylesheet\" href=\"HtmlEncode[[/base.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\" />",
                output.Content.GetContent());
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
                TextWriter.Null);

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
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), It.IsAny<IEntryLink>(), out result))
                .Returns(result != null);

            var cacheSetContext = new Mock<ICacheSetContext>();
            cacheSetContext.Setup(c => c.AddExpirationTrigger(It.IsAny<IExpirationTrigger>()));
            cache
                .Setup(
                    c => c.Set(
                        /*key*/ It.IsAny<string>(),
                        /*link*/ It.IsAny<IEntryLink>(),
                        /*state*/ It.IsAny<object>(),
                        /*create*/ It.IsAny<Func<ICacheSetContext, object>>()))
                .Returns((
                    string input,
                    IEntryLink entryLink,
                    object state,
                    Func<ICacheSetContext, object> create) =>
                {
                    {
                        cacheSetContext.Setup(c => c.State).Returns(state);
                        return create(cacheSetContext.Object);
                    }
                });
            return cache.Object;
        }
    }
}