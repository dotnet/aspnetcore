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
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class ScriptTagHelperTest
    {
        [Theory]
        [InlineData(null, "test.js", "test.js")]
        [InlineData("abcd.js", "test.js", "test.js")]
        [InlineData(null, "~/test.js", "virtualRoot/test.js")]
        [InlineData("abcd.js", "~/test.js", "virtualRoot/test.js")]
        public void Process_SrcDefaultsToTagHelperOutputSrcAttributeAddedByOtherTagHelper(
            string src,
            string srcOutput,
            string expectedSrcPrefix)
        {
            // Arrange
            var allAttributes = new TagHelperAttributeList(
                new TagHelperAttributeList
                {
                    { "type", new HtmlString("text/javascript") },
                    { "asp-append-version", true },
                });
            var context = MakeTagHelperContext(allAttributes);
            var outputAttributes = new TagHelperAttributeList
                {
                    { "type", new HtmlString("text/javascript") },
                    { "src", srcOutput },
                };
            var output = MakeTagHelperOutput("script", outputAttributes);
            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var urlHelper = new Mock<IUrlHelper>();

            // Ensure expanded path does not look like an absolute path on Linux, avoiding
            // https://github.com/aspnet/External/issues/21
            urlHelper
                .Setup(urlhelper => urlhelper.Content(It.IsAny<string>()))
                .Returns(new Func<string, string>(url => url.Replace("~/", "virtualRoot/")));

            var helper = new ScriptTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                urlHelper.Object)
            {
                ViewContext = viewContext,
                AppendVersion = true,
                Src = src,
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(
                expectedSrcPrefix + "?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk",
                (string)output.Attributes["src"].Value,
                StringComparer.Ordinal);
        }

        [Theory]
        [MemberData(nameof(LinkTagHelperTest.MultiAttributeSameNameData), MemberType = typeof(LinkTagHelperTest))]
        public async Task HandlesMultipleAttributesSameNameCorrectly(TagHelperAttributeList outputAttributes)
        {
            // Arrange
            var allAttributes = new TagHelperAttributeList(
                outputAttributes.Concat(
                    new TagHelperAttributeList
                    {
                        ["data-extra"] = "something",
                        ["src"] = "/blank.js",
                        ["asp-fallback-src"] = "http://www.example.com/blank.js",
                        ["asp-fallback-test"] = "isavailable()",
                    }));
            var tagHelperContext = MakeTagHelperContext(allAttributes);
            var viewContext = MakeViewContext();
            var combinedOutputAttributes = new TagHelperAttributeList(
                outputAttributes.Concat(
                    new[]
                    {
                        new TagHelperAttribute("data-extra", new HtmlString("something"))
                    }));
            var output = MakeTagHelperOutput("script", combinedOutputAttributes);
            var hostingEnvironment = MakeHostingEnvironment();

            var helper = new ScriptTagHelper(
                CreateLogger(),
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
                FallbackSrc = "~/blank.js",
                FallbackTestExpression = "http://www.example.com/blank.js",
                Src = "/blank.js",
            };
            var expectedAttributes = new TagHelperAttributeList(output.Attributes);
            expectedAttributes.Add(new TagHelperAttribute("src", "/blank.js"));

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes);
        }

        public static TheoryData RunsWhenRequiredAttributesArePresent_Data
        {
            get
            {
                return new TheoryData<TagHelperAttributeList, Action<ScriptTagHelper>>
                {
                    {
                        new TagHelperAttributeList
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
                        new TagHelperAttributeList
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
                        new TagHelperAttributeList
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
                        new TagHelperAttributeList
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
                    },
                    // File Version
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-src"] = "test.js",
                            ["asp-fallback-test"] = "isavailable()",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrc = "test.js";
                            tagHelper.FallbackTestExpression = "isavailable()";
                            tagHelper.AppendVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-src-include"] = "*.js",
                            ["asp-fallback-test"] = "isavailable()",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrcInclude = "*.css";
                            tagHelper.FallbackTestExpression = "isavailable()";
                            tagHelper.AppendVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-src"] = "test.js",
                            ["asp-fallback-src-include"] = "*.js",
                            ["asp-fallback-test"] = "isavailable()",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrc = "test.js";
                            tagHelper.FallbackSrcInclude = "*.css";
                            tagHelper.FallbackTestExpression = "isavailable()";
                            tagHelper.AppendVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-fallback-src-include"] = "*.js",
                            ["asp-fallback-src-exclude"] = "*.min.js",
                            ["asp-fallback-test"] = "isavailable()",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrcInclude = "*.css";
                            tagHelper.FallbackSrcExclude = "*.min.css";
                            tagHelper.FallbackTestExpression = "isavailable()";
                            tagHelper.AppendVersion = true;
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RunsWhenRequiredAttributesArePresent_Data))]
        public async Task RunsWhenRequiredAttributesArePresent(
            TagHelperAttributeList attributes,
            Action<ScriptTagHelper> setProperties)
        {
            // Arrange
            var context = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { "/common.js" });

            var helper = new ScriptTagHelper(
                CreateLogger(),
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
                GlobbingUrlBuilder = globbingUrlBuilder.Object
            };
            setProperties(helper);

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
            Assert.True(output.PostElement.IsModified);
            Assert.Empty(logger.Logged);
        }

        public static TheoryData RunsWhenRequiredAttributesArePresent_NoSrc_Data
        {
            get
            {
                return new TheoryData<TagHelperAttributeList, Action<ScriptTagHelper>>
                {
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-src-include"] = "*.js"
                        },
                        tagHelper =>
                        {
                            tagHelper.SrcInclude = "*.js";
                        }
                    },
                    {
                        new TagHelperAttributeList
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
                        new TagHelperAttributeList
                        {
                            ["asp-src-include"] = "*.js",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.SrcInclude = "*.js";
                            tagHelper.AppendVersion = true;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            ["asp-src-include"] = "*.js",
                            ["asp-src-exclude"] = "*.min.js",
                            ["asp-append-version"] = "true"
                        },
                        tagHelper =>
                        {
                            tagHelper.SrcInclude = "*.js";
                            tagHelper.SrcExclude = "*.min.js";
                            tagHelper.AppendVersion = true;
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RunsWhenRequiredAttributesArePresent_NoSrc_Data))]
        public async Task RunsWhenRequiredAttributesArePresent_NoSrc(
            TagHelperAttributeList attributes,
            Action<ScriptTagHelper> setProperties)
        {
            // Arrange
            var context = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { "/common.js" });

            var helper = new ScriptTagHelper(
                CreateLogger(),
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
                GlobbingUrlBuilder = globbingUrlBuilder.Object
            };
            setProperties(helper);

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Null(output.TagName);
            Assert.True(output.IsContentModified);
            Assert.True(output.PostElement.IsModified);
            Assert.Empty(logger.Logged);
        }

        public static TheoryData DoesNotRunWhenARequiredAttributeIsMissing_Data
        {
            get
            {
                return new TheoryData<TagHelperAttributeList, Action<ScriptTagHelper>>
                {
                    {
                        new TagHelperAttributeList
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
                        new TagHelperAttributeList
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
                        new TagHelperAttributeList
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
                        new TagHelperAttributeList
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
            TagHelperAttributeList attributes,
            Action<ScriptTagHelper> setProperties)
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("script");
            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new ScriptTagHelper(
                CreateLogger(),
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
            };
            setProperties(helper);

            // Act
            helper.Process(tagHelperContext, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
            Assert.Empty(output.Attributes);
            Assert.True(output.PostElement.IsEmpty);
        }

        [Theory]
        [MemberData(nameof(DoesNotRunWhenARequiredAttributeIsMissing_Data))]
        public async Task LogsWhenARequiredAttributeIsMissing(
            TagHelperAttributeList attributes,
            Action<ScriptTagHelper> setProperties)
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext(attributes);
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new ScriptTagHelper(
                logger,
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
            };
            setProperties(helper);

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.IsContentModified);
            Assert.Empty(output.Attributes);
            Assert.True(output.PostElement.IsEmpty);

            Assert.Equal(2, logger.Logged.Count);

            Assert.Equal(LogLevel.Warning, logger.Logged[0].LogLevel);
            Assert.IsAssignableFrom<ILogValues>(logger.Logged[0].State);

            var loggerData0 = (ILogValues)logger.Logged[0].State;

            Assert.Equal(LogLevel.Verbose, logger.Logged[1].LogLevel);
            Assert.IsAssignableFrom<ILogValues>(logger.Logged[1].State);
            Assert.StartsWith("Skipping processing for tag helper 'Microsoft.AspNet.Mvc.TagHelpers.ScriptTagHelper'" +
                " with id",
                ((ILogValues)logger.Logged[1].State).ToString());
        }

        [Fact]
        public async Task DoesNotRunWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext();
            var viewContext = MakeViewContext();
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            var helper = new ScriptTagHelper(
                CreateLogger(),
                MakeHostingEnvironment(),
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
            };

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.IsContentModified);
            Assert.Empty(output.Attributes);
            Assert.True(output.PostElement.IsEmpty);
        }

        [Fact]
        public async Task LogsWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext();
            var viewContext = MakeViewContext();
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            var helper = new ScriptTagHelper(
                logger,
                MakeHostingEnvironment(),
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
            };

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.IsContentModified);

            Assert.Single(logger.Logged);

            Assert.Equal(LogLevel.Verbose, logger.Logged[0].LogLevel);
            Assert.IsAssignableFrom<ILogValues>(logger.Logged[0].State);
            Assert.StartsWith("Skipping processing for tag helper 'Microsoft.AspNet.Mvc.TagHelpers.ScriptTagHelper'",
                ((ILogValues)logger.Logged[0].State).ToString());
        }

        [Fact]
        public async Task PreservesOrderOfNonSrcAttributes()
        {
            // Arrange
            var tagHelperContext = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    ["data-extra"] = "something",
                    ["src"] = "/blank.js",
                    ["data-more"] = "else",
                    ["asp-fallback-src"] = "http://www.example.com/blank.js",
                    ["asp-fallback-test"] = "isavailable()",
                });

            var viewContext = MakeViewContext();

            var output = MakeTagHelperOutput("src",
                attributes: new TagHelperAttributeList
                {
                    ["data-extra"] = "something",
                    ["data-more"] = "else",
                });

            var logger = CreateLogger();
            var hostingEnvironment = MakeHostingEnvironment();

            var helper = new ScriptTagHelper(
                logger,
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
                FallbackSrc = "~/blank.js",
                FallbackTestExpression = "http://www.example.com/blank.js",
                Src = "/blank.js",
            };

            // Act
            await helper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal("data-extra", output.Attributes[0].Name);
            Assert.Equal("src", output.Attributes[1].Name);
            Assert.Equal("data-more", output.Attributes[2].Name);
            Assert.Empty(logger.Logged);
        }

        [Fact]
        public async Task RendersScriptTagsForGlobbedSrcResults()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    ["src"] = "/js/site.js",
                    ["asp-src-include"] = "**/*.js"
                });
            var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());
            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.js", null))
                .Returns(new[] { "/common.js" });

            var helper = new ScriptTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                ViewContext = viewContext,
                Src = "/js/site.js",
                SrcInclude = "**/*.js",
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.Equal("/js/site.js", output.Attributes["src"].Value);
            Assert.Equal("<script src=\"HtmlEncode[[/common.js]]\"></script>", output.PostElement.GetContent());
        }

        [Fact]
        public async Task RendersScriptTagsForGlobbedSrcResults_UsesProvidedEncoder()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    ["src"] = "/js/site.js",
                    ["asp-src-include"] = "**/*.js"
                });
            var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());
            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.js", null))
                .Returns(new[] { "/common.js" });

            var helper = new ScriptTagHelper(
                logger.Object,
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                ViewContext = viewContext,
                Src = "/js/site.js",
                SrcInclude = "**/*.js",
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.Equal("/js/site.js", output.Attributes["src"].Value);
            Assert.Equal("<script src=\"HtmlEncode[[/common.js]]\"></script>", output.PostElement.GetContent());
        }

        [Fact]
        public async Task RenderScriptTags_WithFileVersion()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    ["src"] = "/js/site.js",
                    ["asp-append-version"] = "true"
                });
            var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new ScriptTagHelper(
                logger.Object,
                MakeHostingEnvironment(),
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
                AppendVersion = true,
                Src = "/js/site.js",
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.Equal("/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
        }

        [Fact]
        public async Task RenderScriptTags_WithFileVersion_AndRequestPathBase()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    ["src"] = "/bar/js/site.js",
                    ["asp-append-version"] = "true"
                });
            var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext("/bar");

            var helper = new ScriptTagHelper(
                logger.Object,
                MakeHostingEnvironment(),
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
                AppendVersion = true,
                Src = "/bar/js/site.js",
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.Equal("/bar/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
        }

        [Fact]
        public async Task RenderScriptTags_FallbackSrc_WithFileVersion()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    ["src"] = "/js/site.js",
                    ["asp-fallback-src-include"] = "fallback.js",
                    ["asp-fallback-test"] = "isavailable()",
                    ["asp-append-version"] = "true"
                });
            var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new ScriptTagHelper(
                logger.Object,
                MakeHostingEnvironment(),
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                ViewContext = viewContext,
                FallbackSrc = "fallback.js",
                FallbackTestExpression = "isavailable()",
                AppendVersion = true,
                Src = "/js/site.js",
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.Equal("/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
            Assert.Equal(Environment.NewLine + "<script>(isavailable()||document.write(\"<script " +
                "src=\\\"JavaScriptEncode[[fallback.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\\\">" +
                "<\\/script>\"));</script>", output.PostElement.GetContent());
        }

        [Fact]
        public async Task RenderScriptTags_GlobbedSrc_WithFileVersion()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    ["src"] = "/js/site.js",
                    ["asp-src-include"] = "*.js",
                    ["asp-append-version"] = "true"
                });
            var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());
            var logger = new Mock<ILogger<ScriptTagHelper>>();
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>();
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "*.js", null))
                .Returns(new[] { "/common.js" });

            var helper = new ScriptTagHelper(
                logger.Object,
                MakeHostingEnvironment(),
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelper())
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                ViewContext = viewContext,
                SrcInclude = "*.js",
                AppendVersion = true,
                Src = "/js/site.js",
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.Equal("/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
            Assert.Equal("<script src=\"HtmlEncode[[/common.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\">" +
                "</script>", output.PostElement.GetContent());
        }

        private TagHelperContext MakeTagHelperContext(
            TagHelperAttributeList attributes = null,
            string content = null)
        {
            attributes = attributes ?? new TagHelperAttributeList();

            return new TagHelperContext(
                attributes,
                items: new Dictionary<object, object>(),
                uniqueId: Guid.NewGuid().ToString("N"));
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

        private TagHelperOutput MakeTagHelperOutput(string tagName, TagHelperAttributeList attributes = null)
        {
            attributes = attributes ?? new TagHelperAttributeList();

            return new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (_) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
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
            mockFileProvider.Setup(fp => fp.Watch(It.IsAny<string>()))
                .Returns(new TestFileChangeToken());
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

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            cacheEntryOptions.AddExpirationToken(Mock.Of<IChangeToken>());
            cache
                .Setup(
                    c => c.Set(
                        /*key*/ It.IsAny<string>(),
                        /*value*/ It.IsAny<object>(),
                        /*options*/ cacheEntryOptions))
                .Returns(result);
            return cache.Object;
        }

        private static IUrlHelper MakeUrlHelper()
        {
            var urlHelper = new Mock<IUrlHelper>();

            urlHelper
                .Setup(helper => helper.Content(It.IsAny<string>()))
                .Returns(new Func<string, string>(url => url));

            return urlHelper.Object;
        }
    }
}
