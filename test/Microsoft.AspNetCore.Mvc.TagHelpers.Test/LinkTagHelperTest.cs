// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers.Internal;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class LinkTagHelperTest
    {
        [Theory]
        [InlineData(null, "test.css", "test.css")]
        [InlineData("abcd.css", "test.css", "test.css")]
        [InlineData(null, "~/test.css", "virtualRoot/test.css")]
        [InlineData("abcd.css", "~/test.css", "virtualRoot/test.css")]
        public void Process_HrefDefaultsToTagHelperOutputHrefAttributeAddedByOtherTagHelper(
            string href,
            string hrefOutput,
            string expectedHrefPrefix)
        {
            // Arrange
            var allAttributes = new TagHelperAttributeList(
                new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "asp-append-version", true },
                });
            var context = MakeTagHelperContext(allAttributes);
            var outputAttributes = new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                    { "href", hrefOutput },
                };
            var output = MakeTagHelperOutput("link", outputAttributes);
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var urlHelper = new Mock<IUrlHelper>();

            // Ensure expanded path does not look like an absolute path on Linux, avoiding
            // https://github.com/aspnet/External/issues/21
            urlHelper
                .Setup(urlhelper => urlhelper.Content(It.IsAny<string>()))
                .Returns(new Func<string, string>(url => url.Replace("~/", "virtualRoot/")));
            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                urlHelperFactory.Object)
            {
                ViewContext = viewContext,
                AppendVersion = true,
                Href = href,
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal(
                expectedHrefPrefix + "?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk",
                (string)output.Attributes["href"].Value,
                StringComparer.Ordinal);
        }

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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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
                            new TagHelperAttribute("asp-fallback-href", "test.css"),
                            new TagHelperAttribute("asp-fallback-test-class", "hidden"),
                            new TagHelperAttribute("asp-fallback-test-property", "visibility"),
                            new TagHelperAttribute("asp-fallback-test-value", "hidden")
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
                            new TagHelperAttribute("asp-fallback-href-include", "*.css"),
                            new TagHelperAttribute("asp-fallback-test-class", "hidden"),
                            new TagHelperAttribute("asp-fallback-test-property", "visibility"),
                            new TagHelperAttribute("asp-fallback-test-value", "hidden")
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
                            new TagHelperAttribute("asp-fallback-href", "test.css"),
                            new TagHelperAttribute("asp-fallback-test-class", "hidden"),
                            new TagHelperAttribute("asp-fallback-test-property", "visibility"),
                            new TagHelperAttribute("asp-fallback-test-value", "hidden"),
                            new TagHelperAttribute("asp-append-version", "true")
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
                            new TagHelperAttribute("asp-fallback-href-include", "*.css"),
                            new TagHelperAttribute("asp-fallback-test-class", "hidden"),
                            new TagHelperAttribute("asp-fallback-test-property", "visibility"),
                            new TagHelperAttribute("asp-fallback-test-value", "hidden"),
                            new TagHelperAttribute("asp-append-version", "true")
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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
                new TestFileProvider(),
                Mock.Of<IMemoryCache>(),
                PathString.Empty);
            globbingUrlBuilder.Setup(g => g.BuildUrlList(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { "/common.css" });

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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
                            new TagHelperAttribute("asp-href-include", "*.css")
                        },
                        tagHelper =>
                        {
                            tagHelper.HrefInclude = "*.css";
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            new TagHelperAttribute("asp-href-include", "*.css"),
                            new TagHelperAttribute("asp-href-exclude", "*.min.css")
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
                            new TagHelperAttribute("asp-href-include", "*.css"),
                            new TagHelperAttribute("asp-append-version", "true")
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
                            new TagHelperAttribute("asp-href-include", "*.css"),
                            new TagHelperAttribute("asp-href-exclude", "*.min.css"),
                            new TagHelperAttribute("asp-append-version", "true")
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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
                new TestFileProvider(),
                Mock.Of<IMemoryCache>(),
                PathString.Empty);
            globbingUrlBuilder.Setup(g => g.BuildUrlList(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new[] { "/common.css" });

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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
            Assert.True(output.Content.GetContent().Length == 0);
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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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
            Assert.Equal("href", output.Attributes[1].Name);
            Assert.Equal("data-extra", output.Attributes[2].Name);
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
                            // This is commented out on purpose: new TagHelperAttribute("asp-href-include", "*.css"),
                            // Note asp-href-include attribute isn't included.
                            new TagHelperAttribute("asp-href-exclude", "*.min.css")
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
                            // This is commented out on purpose: new TagHelperAttribute("asp-fallback-href", "test.css"),
                            // Note asp-href-include attribute isn't included.
                            new TagHelperAttribute("asp-fallback-test-class", "hidden"),
                            new TagHelperAttribute("asp-fallback-test-property", "visibility"),
                            new TagHelperAttribute("asp-fallback-test-value", "hidden")
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
                            new TagHelperAttribute("asp-fallback-href", "test.css"),
                            new TagHelperAttribute("asp-fallback-test-class", "hidden"),
                            // This is commented out on purpose: new TagHelperAttribute("asp-fallback-test-property", "visibility"),
                            // Note asp-href-include attribute isn't included.
                            new TagHelperAttribute("asp-fallback-test-value", "hidden")
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
                            // This is commented out on purpose: new TagHelperAttribute("asp-fallback-href-include", "test.css"),
                            new TagHelperAttribute("asp-fallback-href-exclude", "**/*.min.css"),
                            new TagHelperAttribute("asp-fallback-test-class", "hidden"),
                            new TagHelperAttribute("asp-fallback-test-property", "visibility"),
                            new TagHelperAttribute("asp-fallback-test-value", "hidden")
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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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
            Assert.True(output.PostElement.GetContent().Length == 0);
        }

        [Fact]
        public void DoesNotRunWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var context = MakeTagHelperContext();
            var output = MakeTagHelperOutput("link");
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
            {
                ViewContext = viewContext,
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.IsContentModified);
            Assert.Empty(output.Attributes);
            Assert.True(output.PostElement.GetContent().Length == 0);
        }

        [Fact]
        public void RendersLinkTagsForGlobbedHrefResults()
        {
            // Arrange
            var expectedContent = "<link rel=\"stylesheet\" href=\"HtmlEncode[[/css/site.css]]\" />" +
                "<link rel=\"stylesheet\" href=\"HtmlEncode[[/base.css]]\" />";
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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
                new TestFileProvider(),
                Mock.Of<IMemoryCache>(),
                PathString.Empty);
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.css", null))
                .Returns(new[] { "/base.css" });

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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
            var content = HtmlContentUtilities.HtmlContentToString(output, new HtmlTestEncoder());
            Assert.Equal(expectedContent, content);
        }

        [Fact]
        public void RendersLinkTagsForGlobbedHrefResults_EncodesAsExpected()
        {
            // Arrange
            var expectedContent =
                "<link encoded='contains \"quotes\"' href=\"HtmlEncode[[/css/site.css]]\" " +
                "literal=\"HtmlEncode[[all HTML encoded]]\" " +
                "mixed='HtmlEncode[[HTML encoded]] and contains \"quotes\"' />" +
                "<link encoded='contains \"quotes\"' href=\"HtmlEncode[[/base.css]]\" " +
                "literal=\"HtmlEncode[[all HTML encoded]]\" " +
                "mixed='HtmlEncode[[HTML encoded]] and contains \"quotes\"' />";
            var mixed = new DefaultTagHelperContent();
            mixed.Append("HTML encoded");
            mixed.AppendHtml(" and contains \"quotes\"");
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "asp-href-include", "**/*.css" },
                    { new TagHelperAttribute("encoded", new HtmlString("contains \"quotes\""), HtmlAttributeValueStyle.SingleQuotes) },
                    { "href", "/css/site.css" },
                    { "literal", "all HTML encoded" },
                    { new TagHelperAttribute("mixed", mixed, HtmlAttributeValueStyle.SingleQuotes) },
                });
            var output = MakeTagHelperOutput(
                "link",
                attributes: new TagHelperAttributeList
                {
                    { new TagHelperAttribute("encoded", new HtmlString("contains \"quotes\""), HtmlAttributeValueStyle.SingleQuotes) },
                    { "literal", "all HTML encoded" },
                    { new TagHelperAttribute("mixed", mixed, HtmlAttributeValueStyle.SingleQuotes) },
                });
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
                new TestFileProvider(),
                Mock.Of<IMemoryCache>(),
                PathString.Empty);
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.css", null))
                .Returns(new[] { "/base.css" });

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
            {
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Href = "/css/site.css",
                HrefInclude = "**/*.css",
                ViewContext = viewContext,
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("link", output.TagName);
            Assert.Equal("/css/site.css", output.Attributes["href"].Value);
            var content = HtmlContentUtilities.HtmlContentToString(output, new HtmlTestEncoder());
            Assert.Equal(expectedContent, content);
        }

        [Fact]
        public void RendersLinkTags_WithFileVersion()
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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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

        [Fact]
        public void RendersLinkTags_WithFileVersion_AndRequestPathBase()
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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext("/bar");

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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

        [Fact]
        public void RenderLinkTags_FallbackHref_WithFileVersion()
        {
            // Arrange
            var expectedPostElement = Environment.NewLine +
                "<meta name=\"x-stylesheet-fallback-test\" content=\"\" class=\"hidden\" /><script>!function" +
                "(a,b,c,d){var e,f=document,g=f.getElementsByTagName(\"SCRIPT\"),h=g[g.length-1]." +
                "previousElementSibling,i=f.defaultView&&f.defaultView.getComputedStyle?f.defaultView." +
                "getComputedStyle(h):h.currentStyle;if(i&&i[a]!==b)for(e=0;e<c.length;e++)f.write('<link " +
                "href=\"'+c[e]+'\" '+d+\"/>\")}(\"JavaScriptEncode[[visibility]]\",\"JavaScriptEncode[[hidden]]\"" +
                ",[\"JavaScriptEncode[[HtmlEncode[[/fallback.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]]]\"]," +
                " \"JavaScriptEncode[[rel=\"stylesheet\" ]]\");</script>";
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "asp-append-version", "true" },
                    { "asp-fallback-href-include", "**/fallback.css" },
                    { "asp-fallback-test-class", "hidden" },
                    { "asp-fallback-test-property", "visibility" },
                    { "asp-fallback-test-value", "hidden" },
                    { "href", "/css/site.css" },
                    { "rel", new HtmlString("stylesheet") },
                });
            var output = MakeTagHelperOutput(
                "link",
                attributes: new TagHelperAttributeList
                {
                    { "rel", new HtmlString("stylesheet") },
                });
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
                new TestFileProvider(),
                Mock.Of<IMemoryCache>(),
                PathString.Empty);
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/fallback.css", null))
                .Returns(new[] { "/fallback.css" });

            var helper = new LinkTagHelper(
                MakeHostingEnvironment(),
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
            {
                AppendVersion = true,
                Href = "/css/site.css",
                FallbackHrefInclude = "**/fallback.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visibility",
                FallbackTestValue = "hidden",
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                ViewContext = viewContext,
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("link", output.TagName);
            Assert.Equal("/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["href"].Value);
            Assert.Equal(expectedPostElement, output.PostElement.GetContent());
        }

        [Fact]
        public void RenderLinkTags_FallbackHref_WithFileVersion_EncodesAsExpected()
        {
            // Arrange
            var expectedContent = "<link encoded=\"contains \"quotes\"\" " +
                "href=\"HtmlEncode[[/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\" " +
                "literal=\"HtmlEncode[[all HTML encoded]]\" " +
                "mixed=\"HtmlEncode[[HTML encoded]] and contains \"quotes\"\" rel=\"stylesheet\" />" +
                Environment.NewLine +
                "<meta name=\"x-stylesheet-fallback-test\" content=\"\" class=\"HtmlEncode[[hidden]]\" /><script>" +
                "!function(a,b,c,d){var e,f=document,g=f.getElementsByTagName(\"SCRIPT\"),h=g[g.length-1]." +
                "previousElementSibling,i=f.defaultView&&f.defaultView.getComputedStyle?f.defaultView." +
                "getComputedStyle(h):h.currentStyle;if(i&&i[a]!==b)for(e=0;e<c.length;e++)f.write('<link " +
                "href=\"'+c[e]+'\" '+d+\"/>\")}(\"JavaScriptEncode[[visibility]]\",\"JavaScriptEncode[[hidden]]\"," +
                "[\"JavaScriptEncode[[HtmlEncode[[/fallback.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]]]\"], " +
                "\"JavaScriptEncode[[encoded=\"contains \"quotes\"\" literal=\"HtmlEncode[[all HTML encoded]]\" " +
                "mixed=\"HtmlEncode[[HTML encoded]] and contains \"quotes\"\" rel=\"stylesheet\" ]]\");" +
                "</script>";
            var mixed = new DefaultTagHelperContent();
            mixed.Append("HTML encoded");
            mixed.AppendHtml(" and contains \"quotes\"");
            var context = MakeTagHelperContext(
                attributes: new TagHelperAttributeList
                {
                    { "asp-append-version", "true" },
                    { "asp-fallback-href-include", "**/fallback.css" },
                    { "asp-fallback-test-class", "hidden" },
                    { "asp-fallback-test-property", "visibility" },
                    { "asp-fallback-test-value", "hidden" },
                    { "encoded", new HtmlString("contains \"quotes\"") },
                    { "href", "/css/site.css" },
                    { "literal", "all HTML encoded" },
                    { "mixed", mixed },
                    { "rel", new HtmlString("stylesheet") },
                });
            var output = MakeTagHelperOutput(
                "link",
                attributes: new TagHelperAttributeList
                {
                    { "encoded", new HtmlString("contains \"quotes\"") },
                    { "literal", "all HTML encoded" },
                    { "mixed", mixed },
                    { "rel", new HtmlString("stylesheet") },
                });
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
                new TestFileProvider(),
                Mock.Of<IMemoryCache>(),
                PathString.Empty);
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/fallback.css", null))
                .Returns(new[] { "/fallback.css" });

            var helper = new LinkTagHelper(
                MakeHostingEnvironment(),
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
            {
                AppendVersion = true,
                FallbackHrefInclude = "**/fallback.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visibility",
                FallbackTestValue = "hidden",
                GlobbingUrlBuilder = globbingUrlBuilder.Object,
                Href = "/css/site.css",
                ViewContext = viewContext,
            };

            // Act
            helper.Process(context, output);

            // Assert
            Assert.Equal("link", output.TagName);
            Assert.Equal("/css/site.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["href"].Value);
            var content = HtmlContentUtilities.HtmlContentToString(output, new HtmlTestEncoder());
            Assert.Equal(expectedContent, content);
        }

        [Fact]
        public void RendersLinkTags_GlobbedHref_WithFileVersion()
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
            var hostingEnvironment = MakeHostingEnvironment();
            var viewContext = MakeViewContext();
            var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
                new TestFileProvider(),
                Mock.Of<IMemoryCache>(),
                PathString.Empty);
            globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.css", null))
                .Returns(new[] { "/base.css" });

            var helper = new LinkTagHelper(
                hostingEnvironment,
                MakeCache(),
                new HtmlTestEncoder(),
                new JavaScriptTestEncoder(),
                MakeUrlHelperFactory())
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
            var content = HtmlContentUtilities.HtmlContentToString(output.PostElement, new HtmlTestEncoder());
            Assert.Equal(
                "<link rel=\"stylesheet\" href=\"HtmlEncode[[/base.css?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\" />",
                content);
        }

        private static ViewContext MakeViewContext(string requestPathBase = null)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            if (requestPathBase != null)
            {
                actionContext.HttpContext.Request.PathBase = new Http.PathString(requestPathBase);
            }

            var metadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());

            return viewContext;
        }

        private static TagHelperContext MakeTagHelperContext(TagHelperAttributeList attributes = null)
        {
            attributes = attributes ?? new TagHelperAttributeList();

            return new TagHelperContext(
                tagName: "link",
                allAttributes: attributes,
                items: new Dictionary<object, object>(),
                uniqueId: Guid.NewGuid().ToString("N"));
        }

        private static TagHelperOutput MakeTagHelperOutput(string tagName, TagHelperAttributeList attributes = null)
        {
            attributes = attributes ?? new TagHelperAttributeList();

            return new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()))
            {
                TagMode = TagMode.SelfClosing,
            };
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

        private static IMemoryCache MakeCache() => new MemoryCache(new MemoryCacheOptions());

        private static IUrlHelperFactory MakeUrlHelperFactory()
        {
            var urlHelper = new Mock<IUrlHelper>();

            urlHelper
                .Setup(helper => helper.Content(It.IsAny<string>()))
                .Returns(new Func<string, string>(url => url));
            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            return urlHelperFactory.Object;
        }
    }
}
