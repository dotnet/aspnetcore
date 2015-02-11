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
    public class ScriptTagHelperTest
    {
        public static TheoryData RunsWhenRequiredAttributesArePresent_Data
        {
            get
            {
                return new TheoryData<IDictionary<string, object>, Action<ScriptTagHelper>>
                {
                    {
                        new Dictionary<string, object>
                        {
                            ["asp-src-include"] = "*.js"
                        },
                        tagHelper =>
                        {
                            tagHelper.SrcInclude = "*.js";
                        }
                    },
                    {
                        new Dictionary<string, object>
                        {
                            ["asp-src-include"] = "*.js",
                            ["asp-src-exclude"] = "*.min.js"
                        },
                        tagHelper =>
                        {
                            tagHelper.SrcInclude = "*.js";
                            tagHelper.SrcExclude = "*.min.js";
                        }
                    },
                    {
                        new Dictionary<string, object>
                        {
                            ["asp-fallback-src"] = "test.js",
                            ["asp-fallback-test"] = "isavailable()"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrc = "test.js";
                            tagHelper.FallbackTestExpression = "isavailable()";
                        }
                    },
                    {
                        new Dictionary<string, object>
                        {
                            ["asp-fallback-src-include"] = "*.js",
                            ["asp-fallback-test"] = "isavailable()"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrcInclude = "*.css";
                            tagHelper.FallbackTestExpression = "isavailable()";
                        }
                    },
                    {
                        new Dictionary<string, object>
                        {
                            ["asp-fallback-src"] = "test.js",
                            ["asp-fallback-src-include"] = "*.js",
                            ["asp-fallback-test"] = "isavailable()"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrc = "test.js";
                            tagHelper.FallbackSrcInclude = "*.css";
                            tagHelper.FallbackTestExpression = "isavailable()";
                        }
                    },
                    {
                        new Dictionary<string, object>
                        {
                            ["asp-fallback-src-include"] = "*.js",
                            ["asp-fallback-src-exclude"] = "*.min.js",
                            ["asp-fallback-test"] = "isavailable()"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrcInclude = "*.css";
                            tagHelper.FallbackSrcExclude = "*.min.css";
                            tagHelper.FallbackTestExpression = "isavailable()";
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RunsWhenRequiredAttributesArePresent_Data))]
        public async Task RunsWhenRequiredAttributesArePresent(
            IDictionary<string, object> attributes,
            Action<ScriptTagHelper> setProperties)
        {
            // Arrange
            var context = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var helper = new ScriptTagHelper
            {
                HtmlEncoder = new HtmlEncoder(),
                Logger = logger,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
            };
            setProperties(helper);

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Null(output.TagName);
            Assert.True(output.IsContentModified);
            Assert.Empty(logger.Logged);
        }

        public static TheoryData DoesNotRunWhenARequiredAttributeIsMissing_Data
        {
            get
            {
                return new TheoryData<IDictionary<string, object>, Action<ScriptTagHelper>>
                {
                    {
                        new Dictionary<string, object>
                        {
                            // This is commented out on purpose: ["asp-src-include"] = "*.js",
                            ["asp-src-exclude"] = "*.min.js"
                        },
                        tagHelper =>
                        {
                            // This is commented out on purpose: tagHelper.SrcInclude = "*.js";
                            tagHelper.SrcExclude = "*.min.js";
                        }
                    },
                    {
                        new Dictionary<string, object>
                        {
                            // This is commented out on purpose: ["asp-fallback-src"] = "test.js",
                            ["asp-fallback-test"] = "isavailable()",
                        },
                        tagHelper =>
                        {
                            // This is commented out on purpose: tagHelper.FallbackSrc = "test.js";
                            tagHelper.FallbackTestExpression = "isavailable()";
                        }
                    },
                    {
                        new Dictionary<string, object>
                        {
                            ["asp-fallback-src"] = "test.js",
                            // This is commented out on purpose: ["asp-fallback-test"] = "isavailable()"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrc = "test.js";
                            // This is commented out on purpose: tagHelper.FallbackTestExpression = "isavailable()";
                        }
                    },
                    {
                        new Dictionary<string, object>
                        {
                            // This is commented out on purpose: ["asp-fallback-src-include"] = "test.js",
                            ["asp-fallback-src-exclude"] = "**/*.min.js",
                            ["asp-fallback-test"] = "isavailable()",
                        },
                        tagHelper =>
                        {
                            // This is commented out on purpose: tagHelper.FallbackSrcInclude = "test.js";
                            tagHelper.FallbackSrcExclude = "**/*.min.js";
                            tagHelper.FallbackTestExpression = "isavailable()";
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(DoesNotRunWhenARequiredAttributeIsMissing_Data))]
        public void DoesNotRunWhenARequiredAttributeIsMissing(
            IDictionary<string, object> attributes,
            Action<ScriptTagHelper> setProperties)
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("script");
            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var helper = new ScriptTagHelper
            {
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext
            };
            setProperties(helper);

            // Act
            helper.Process(tagHelperContext, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
        }

        [Theory]
        [MemberData(nameof(DoesNotRunWhenARequiredAttributeIsMissing_Data))]
        public async Task LogsWhenARequiredAttributeIsMissing(
            IDictionary<string, object> attributes,
            Action<ScriptTagHelper> setProperties)
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var helper = new ScriptTagHelper
            {
                Logger = logger,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext
            };
            setProperties(helper);

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.IsContentModified);

            Assert.Equal(2, logger.Logged.Count);

            Assert.Equal(LogLevel.Warning, logger.Logged[0].LogLevel);
            Assert.IsAssignableFrom<PartialModeMatchLoggerStructure>(logger.Logged[0].State);

            var loggerData0 = (PartialModeMatchLoggerStructure)logger.Logged[0].State;

            Assert.Equal(LogLevel.Verbose, logger.Logged[1].LogLevel);
            Assert.IsAssignableFrom<ILogValues>(logger.Logged[1].State);
            Assert.StartsWith("Skipping processing for Microsoft.AspNet.Mvc.TagHelpers.ScriptTagHelper",
                ((ILogValues)logger.Logged[1].State).Format());
        }

        [Fact]
        public async Task DoesNotRunWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext();
            var viewContext = MakeViewContext();
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            var helper = new ScriptTagHelper
            {
                Logger = logger,
                ViewContext = viewContext
            };

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.IsContentModified);
        }

        [Fact]
        public async Task LogsWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext();
            var viewContext = MakeViewContext();
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            var helper = new ScriptTagHelper
            {
                Logger = logger,
                ViewContext = viewContext
            };

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.IsContentModified);

            Assert.Single(logger.Logged);

            Assert.Equal(LogLevel.Verbose, logger.Logged[0].LogLevel);
            Assert.IsAssignableFrom<ILogValues>(logger.Logged[0].State);
            Assert.StartsWith("Skipping processing for Microsoft.AspNet.Mvc.TagHelpers.ScriptTagHelper",
                ((ILogValues)logger.Logged[0].State).Format());
        }

        [Fact]
        public async Task PreservesOrderOfSourceAttributesWhenRun()
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext(
                attributes: new Dictionary<string, object>
                {
                    ["data-extra"] = "something",
                    ["src"] = "/blank.js",
                    ["data-more"] = "else",
                    ["asp-fallback-src"] = "http://www.example.com/blank.js",
                    ["asp-fallback-test"] = "isavailable()",
                });

            var viewContext = MakeViewContext();

            var output = MakeTagHelperOutput("src",
                attributes: new Dictionary<string, string>
                {
                    ["data-extra"] = "something",
                    ["src"] = "/blank.js",
                    ["data-more"] = "else",
                });

            var logger = CreateLogger();
            var hostingEnvironment = MakeHostingEnvironment();

            var helper = new ScriptTagHelper
            {
                HtmlEncoder = new HtmlEncoder(),
                Logger = logger,
                ViewContext = viewContext,
                HostingEnvironment = hostingEnvironment,
                FallbackSrc = "~/blank.js",
                FallbackTestExpression = "http://www.example.com/blank.js",
            };

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.StartsWith(
                "<script data-extra=\"something\" src=\"/blank.js\" data-more=\"else\"", output.Content.GetContent());
            Assert.Empty(logger.Logged);
        }

        [Fact]
        public async Task RendersScriptTagsForGlobbedSrcResults()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new Dictionary<string, object>
                {
                    ["src"] = "/js/site.js",
                    ["asp-src-include"] = "**/*.js"
                });
            var output = MakeTagHelperOutput("script", attributes: new Dictionary<string, string>
            {
                ["src"] = "/js/site.js"
            });
            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList("/js/site.js", "**/*.js", null))
                .Returns(new[] { "/js/site.js", "/common.js" });
            var helper = new ScriptTagHelper
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                SrcInclude = "**/*.js",
                HtmlEncoder = new HtmlEncoder()
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("<script src=\"/js/site.js\"></script><script src=\"/common.js\"></script>", output.Content.GetContent());
        }

        [Fact]
        public async Task RendersScriptTagsForGlobbedSrcResults_UsesProvidedEncoder()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new Dictionary<string, object>
                {
                    ["src"] = "/js/site.js",
                    ["asp-src-include"] = "**/*.js"
                });
            var output = MakeTagHelperOutput("script", attributes: new Dictionary<string, string>
            {
                ["src"] = "/js/site.js"
            });
            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList("/js/site.js", "**/*.js", null))
                .Returns(new[] { "/js/site.js", "/common.js" });
            var helper = new ScriptTagHelper
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Logger = logger.Object,
                HostingEnvironment = hostingEnvironment,
                ViewContext = viewContext,
                SrcInclude = "**/*.js",
                HtmlEncoder = new TestHtmlEncoder()
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("<script src=\"HtmlEncode[[/js/site.js]]\"></script>" +
                "<script src=\"HtmlEncode[[/common.js]]\"></script>", output.Content.GetContent());
        }

        private TagHelperContext MakeTagHelperContext(
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

        private TagHelperOutput MakeTagHelperOutput(string tagName, IDictionary<string, string> attributes = null)
        {
            attributes = attributes ?? new Dictionary<string, string>();

            return new TagHelperOutput(tagName, attributes, new HtmlEncoder());
        }

        private TagHelperLogger<ScriptTagHelper> CreateLogger()
        {
            return new TagHelperLogger<ScriptTagHelper>();
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