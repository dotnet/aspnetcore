// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Logging;
using Microsoft.Framework.WebEncoders;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class LinkTagHelperTest
    {
        public static TheoryData RunsWhenRequiredAttributesArePresent_Data
        {
            get
            {
                return new TheoryData<IDictionary<string, object>, Action<LinkTagHelper>>
                {
                    {
                        new Dictionary<string, object>
                        {
                            ["asp-href-include"] = "*.css"
                        },
                        tagHelper =>
                        {
                            tagHelper.HrefInclude = "*.css";
                        }
                    },
                    {
                        new Dictionary<string, object>
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
                        new Dictionary<string, object>
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
                        new Dictionary<string, object>
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
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RunsWhenRequiredAttributesArePresent_Data))]
        public void RunsWhenRequiredAttributesArePresent(
            IDictionary<string, object> attributes,
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
                HtmlEncoder = new HtmlEncoder(),
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
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
                attributes: new Dictionary<string, object>
                {
                    ["rel"] = "stylesheet",
                    ["data-extra"] = "something",
                    ["href"] = "test.css",
                    ["asp-fallback-href"] = "test.css",
                    ["asp-fallback-test-class"] = "hidden",
                    ["asp-fallback-test-property"] = "visibility",
                    ["asp-fallback-test-value"] = "hidden"
                });
            var output = MakeTagHelperOutput("link",
                attributes: new Dictionary<string, string>
                {
                    ["rel"] = "stylesheet",
                    ["data-extra"] = "something",
                    ["href"] = "test.css"
                });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new HtmlEncoder(),
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                FallbackHref = "test.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visibility",
                FallbackTestValue = "hidden"
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.StartsWith(
                "<link rel=\"stylesheet\" data-extra=\"something\" href=\"test.css\"", output.Content.GetContent());
        }

        public static TheoryData DoesNotRunWhenARequiredAttributeIsMissing_Data
        {
            get
            {
                return new TheoryData<IDictionary<string, object>, Action<LinkTagHelper>>
                {
                    {
                        new Dictionary<string, object>
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
                        new Dictionary<string, object>
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
                        new Dictionary<string, object>
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
                        new Dictionary<string, object>
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
            IDictionary<string, object> attributes,
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
                ViewContext = viewContext
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
                ViewContext = viewContext
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
                attributes: new Dictionary<string, object>
                {
                    ["href"] = "/css/site.css",
                    ["rel"] = "stylesheet",
                    ["asp-href-include"] = "**/*.css"
                });
            var output = MakeTagHelperOutput("link", attributes: new Dictionary<string, string>
            {
                ["href"] = "/css/site.css",
                ["rel"] = "stylesheet"
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList("/css/site.css", "**/*.css", null))
                .Returns(new[] { "/css/site.css", "/base.css" });
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new HtmlEncoder(),
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                HrefInclude = "**/*.css"
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("<link href=\"/css/site.css\" rel=\"stylesheet\" />" +
                         "<link href=\"/base.css\" rel=\"stylesheet\" />", output.Content.GetContent());
        }

        [Fact]
        public void RendersLinkTagsForGlobbedHrefResults_UsingProvidedEncoder()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new Dictionary<string, object>
                {
                    ["href"] = "/css/site.css",
                    ["rel"] = "stylesheet",
                    ["asp-href-include"] = "**/*.css"
                });
            var output = MakeTagHelperOutput("link", attributes: new Dictionary<string, string>
            {
                ["href"] = "/css/site.css",
                ["rel"] = "stylesheet"
            });
            var logger = new Mock<ILogger<LinkTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList("/css/site.css", "**/*.css", null))
                .Returns(new[] { "/css/site.css", "/base.css" });
            var helper = new LinkTagHelper
            {
                HtmlEncoder = new TestHtmlEncoder(),
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                HrefInclude = "**/*.css"
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("<link href=\"HtmlEncode[[/css/site.css]]\" rel=\"stylesheet\" />" +
                         "<link href=\"HtmlEncode[[/base.css]]\" rel=\"stylesheet\" />", output.Content.GetContent());
        }

        private static ViewContext MakeViewContext()
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
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
            IDictionary<string, object> attributes = null,
            string content = null)
        {
            attributes = attributes ?? new Dictionary<string, object>();

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

        private static TagHelperOutput MakeTagHelperOutput(string tagName, IDictionary<string, string> attributes = null)
        {
            attributes = attributes ?? new Dictionary<string, string>();

            return new TagHelperOutput(tagName, attributes, new HtmlEncoder());
        }

        private static IHostingEnvironment MakeHostingEnvironment()
        {
            var emptyDirectoryContents = new Mock<IDirectoryContents>();
            emptyDirectoryContents.Setup(dc => dc.GetEnumerator())
                .Returns(Enumerable.Empty<IFileInfo>().GetEnumerator());
            var mockFileProvider = new Mock<IFileProvider>();
            mockFileProvider.Setup(fp => fp.GetDirectoryContents(It.IsAny<string>()))
                .Returns(emptyDirectoryContents.Object);
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.Setup(h => h.WebRootFileProvider).Returns(mockFileProvider.Object);

            return hostingEnvironment.Object;
        }

        private class TestHtmlEncoder : IHtmlEncoder
        {
            public string HtmlEncode(string value)
            {
                return "HtmlEncode[[" + value + "]]";
            }

            public void HtmlEncode(string value, int startIndex, int charCount, TextWriter output)
            {
                output.Write("HtmlEncode[[");
                output.Write(value.Substring(startIndex, charCount));
                output.Write("]]");
            }

            public void HtmlEncode(char[] value, int startIndex, int charCount, TextWriter output)
            {
                output.Write("HtmlEncode[[");
                output.Write(value, startIndex, charCount);
                output.Write("]]");
            }
        }
    }
}