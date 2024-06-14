// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

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

        var helper = GetHelper(urlHelperFactory: urlHelperFactory.Object);
        helper.AppendVersion = true;
        helper.Src = src;

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
    public void HandlesMultipleAttributesSameNameCorrectly(TagHelperAttributeList outputAttributes)
    {
        // Arrange
        var allAttributes = new TagHelperAttributeList(
            outputAttributes.Concat(
                new TagHelperAttributeList
                {
                        new TagHelperAttribute("data-extra", "something"),
                        new TagHelperAttribute("src", "/blank.js"),
                        new TagHelperAttribute("asp-fallback-src", "http://www.example.com/blank.js"),
                        new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                }));
        var tagHelperContext = MakeTagHelperContext(allAttributes);
        var combinedOutputAttributes = new TagHelperAttributeList(
            outputAttributes.Concat(
                new[]
                {
                        new TagHelperAttribute("data-extra", new HtmlString("something"))
                }));
        var output = MakeTagHelperOutput("script", combinedOutputAttributes);

        var helper = GetHelper();
        helper.FallbackSrc = "~/blank.js";
        helper.FallbackTestExpression = "http://www.example.com/blank.js";
        helper.Src = "/blank.js";

        var expectedAttributes = new TagHelperAttributeList(output.Attributes);
        expectedAttributes.Add(new TagHelperAttribute("src", "/blank.js"));

        // Act
        helper.Process(tagHelperContext, output);

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
                            new TagHelperAttribute("asp-fallback-src", "test.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()")
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
                            new TagHelperAttribute("asp-fallback-src", "test.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                            new TagHelperAttribute("asp-suppress-fallback-integrity", "false")
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrc = "test.js";
                            tagHelper.FallbackTestExpression = "isavailable()";
                            tagHelper.SuppressFallbackIntegrity = false;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            new TagHelperAttribute("asp-fallback-src-include", "*.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()")
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
                            new TagHelperAttribute("asp-fallback-src", "test.js"),
                            new TagHelperAttribute("asp-fallback-src-include", "*.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()")
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
                            new TagHelperAttribute("asp-fallback-src", "test.js"),
                            new TagHelperAttribute("asp-fallback-src-include", "*.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                            new TagHelperAttribute("asp-suppress-fallback-integrity", "false")
                        },
                        tagHelper =>
                        {
                            tagHelper.FallbackSrc = "test.js";
                            tagHelper.FallbackSrcInclude = "*.css";
                            tagHelper.FallbackTestExpression = "isavailable()";
                            tagHelper.SuppressFallbackIntegrity = false;
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            new TagHelperAttribute("asp-fallback-src-include", "*.js"),
                            new TagHelperAttribute("asp-fallback-src-exclude", "*.min.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()")
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
                            new TagHelperAttribute("asp-fallback-src", "test.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                            new TagHelperAttribute("asp-append-version", "true")
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
                            new TagHelperAttribute("asp-fallback-src-include", "*.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                            new TagHelperAttribute("asp-append-version", "true")
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
                            new TagHelperAttribute("asp-fallback-src", "test.js"),
                            new TagHelperAttribute("asp-fallback-src-include", "*.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                            new TagHelperAttribute("asp-append-version", "true")
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
                            new TagHelperAttribute("asp-fallback-src-include", "*.js"),
                            new TagHelperAttribute("asp-fallback-src-exclude", "*.min.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                            new TagHelperAttribute("asp-append-version", "true")
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
    public void RunsWhenRequiredAttributesArePresent(
        TagHelperAttributeList attributes,
        Action<ScriptTagHelper> setProperties)
    {
        // Arrange
        var context = MakeTagHelperContext(attributes);
        var output = MakeTagHelperOutput("script");
        var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
            new TestFileProvider(),
            Mock.Of<IMemoryCache>(),
            PathString.Empty);
        globbingUrlBuilder.Setup(g => g.BuildUrlList(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new[] { "/common.js" });

        var helper = GetHelper();
        helper.GlobbingUrlBuilder = globbingUrlBuilder.Object;
        setProperties(helper);

        // Act
        helper.Process(context, output);

        // Assert
        Assert.NotNull(output.TagName);
        Assert.False(output.IsContentModified);
        Assert.True(output.PostElement.IsModified);
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
                            new TagHelperAttribute("asp-src-include", "*.js")
                        },
                        tagHelper =>
                        {
                            tagHelper.SrcInclude = "*.js";
                        }
                    },
                    {
                        new TagHelperAttributeList
                        {
                            new TagHelperAttribute("asp-src-include", "*.js"),
                            new TagHelperAttribute("asp-src-exclude", "*.min.js")
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
                            new TagHelperAttribute("asp-src-include", "*.js"),
                            new TagHelperAttribute("asp-append-version", "true")
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
                            new TagHelperAttribute("asp-src-include", "*.js"),
                            new TagHelperAttribute("asp-src-exclude", "*.min.js"),
                            new TagHelperAttribute("asp-append-version", "true")
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
    public void RunsWhenRequiredAttributesArePresent_NoSrc(
        TagHelperAttributeList attributes,
        Action<ScriptTagHelper> setProperties)
    {
        // Arrange
        var context = MakeTagHelperContext(attributes);
        var output = MakeTagHelperOutput("script");
        var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
            new TestFileProvider(),
            Mock.Of<IMemoryCache>(),
            PathString.Empty);
        globbingUrlBuilder.Setup(g => g.BuildUrlList(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new[] { "/common.js" });

        var helper = GetHelper();
        helper.GlobbingUrlBuilder = globbingUrlBuilder.Object;
        setProperties(helper);

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.True(output.IsContentModified);
        Assert.True(output.Content.GetContent().Length == 0);
        Assert.True(output.PostElement.IsModified);
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
                            // This is commented out on purpose: new TagHelperAttribute("asp-src-include", "*.js"),
                            // Note asp-src-include attribute isn't included.
                            new TagHelperAttribute("asp-src-exclude", "*.min.js")
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
                            // This is commented out on purpose: new TagHelperAttribute("asp-fallback-src", "test.js"),
                            // Note asp-src-include attribute isn't included.
                            new TagHelperAttribute("asp-fallback-test", "isavailable()"),
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
                            new TagHelperAttribute("asp-fallback-src", "test.js"),
                            // This is commented out on purpose: new TagHelperAttribute("asp-fallback-test", "isavailable()")
                            // Note asp-src-include attribute isn't included.
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
                            // This is commented out on purpose: new TagHelperAttribute("asp-fallback-src-include", "test.js"),
                            // Note asp-src-include attribute isn't included.
                            new TagHelperAttribute("asp-fallback-src-exclude", "**/*.min.js"),
                            new TagHelperAttribute("asp-fallback-test", "isavailable()"),
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

        var helper = GetHelper();
        setProperties(helper);

        // Act
        helper.Process(tagHelperContext, output);

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
        var tagHelperContext = MakeTagHelperContext();
        var viewContext = MakeViewContext();
        var output = MakeTagHelperOutput("script");

        var helper = GetHelper();

        // Act
        helper.Process(tagHelperContext, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.False(output.IsContentModified);
        Assert.Empty(output.Attributes);
        Assert.True(output.PostElement.GetContent().Length == 0);
    }

    [Fact]
    public void PreservesOrderOfNonSrcAttributes()
    {
        // Arrange
        var tagHelperContext = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("data-extra", "something"),
                    new TagHelperAttribute("src", "/blank.js"),
                    new TagHelperAttribute("data-more", "else"),
                    new TagHelperAttribute("asp-fallback-src", "http://www.example.com/blank.js"),
                    new TagHelperAttribute("asp-fallback-test", "isavailable()"),
            });

        var output = MakeTagHelperOutput("src",
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("data-extra", "something"),
                    new TagHelperAttribute("data-more", "else"),
            });

        var helper = GetHelper();
        helper.FallbackSrc = "~/blank.js";
        helper.FallbackTestExpression = "http://www.example.com/blank.js";
        helper.Src = "/blank.js";

        // Act
        helper.Process(tagHelperContext, output);

        // Assert
        Assert.Equal("data-extra", output.Attributes[0].Name);
        Assert.Equal("src", output.Attributes[1].Name);
        Assert.Equal("data-more", output.Attributes[2].Name);
    }

    [Fact]
    public void RendersScriptTagsForGlobbedSrcResults()
    {
        // Arrange
        var expectedContent = "<script src=\"HtmlEncode[[/js/site.js]]\"></script>" +
            "<script src=\"HtmlEncode[[/common.js]]\"></script>";
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", "/js/site.js"),
                    new TagHelperAttribute("asp-src-include", "**/*.js")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());
        var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
            new TestFileProvider(),
            Mock.Of<IMemoryCache>(),
            PathString.Empty);
        globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.js", null))
            .Returns(new[] { "/common.js" });

        var helper = GetHelper();
        helper.GlobbingUrlBuilder = globbingUrlBuilder.Object;
        helper.Src = "/js/site.js";
        helper.SrcInclude = "**/*.js";

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/js/site.js", output.Attributes["src"].Value);
        var content = HtmlContentUtilities.HtmlContentToString(output, new HtmlTestEncoder());
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public void RendersScriptTagsForGlobbedSrcResults_EncodesAsExpected()
    {
        // Arrange
        var expectedContent =
            "<script encoded='contains \"quotes\"' literal=\"HtmlEncode[[all HTML encoded]]\" " +
            "mixed='HtmlEncode[[HTML encoded]] and contains \"quotes\"' " +
            "src=\"HtmlEncode[[/js/site.js]]\"></script>" +
            "<script encoded='contains \"quotes\"' literal=\"HtmlEncode[[all HTML encoded]]\" " +
            "mixed='HtmlEncode[[HTML encoded]] and contains \"quotes\"' " +
            "src=\"HtmlEncode[[/common.js]]\"></script>";
        var mixed = new DefaultTagHelperContent();
        mixed.Append("HTML encoded");
        mixed.AppendHtml(" and contains \"quotes\"");
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    { "asp-src-include", "**/*.js" },
                    { new TagHelperAttribute("encoded", new HtmlString("contains \"quotes\""), HtmlAttributeValueStyle.SingleQuotes) },
                    { "literal", "all HTML encoded" },
                    { new TagHelperAttribute("mixed", mixed, HtmlAttributeValueStyle.SingleQuotes) },
                    { "src", "/js/site.js" },
            });
        var output = MakeTagHelperOutput(
            "script",
            attributes: new TagHelperAttributeList
            {
                    { new TagHelperAttribute("encoded", new HtmlString("contains \"quotes\""), HtmlAttributeValueStyle.SingleQuotes) },
                    { "literal", "all HTML encoded"},
                    { new TagHelperAttribute("mixed", mixed, HtmlAttributeValueStyle.SingleQuotes) },
            });
        var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
            new TestFileProvider(),
            Mock.Of<IMemoryCache>(),
            PathString.Empty);
        globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "**/*.js", null))
            .Returns(new[] { "/common.js" });

        var helper = GetHelper();
        helper.GlobbingUrlBuilder = globbingUrlBuilder.Object;
        helper.Src = "/js/site.js";
        helper.SrcInclude = "**/*.js";

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/js/site.js", output.Attributes["src"].Value);
        var content = HtmlContentUtilities.HtmlContentToString(output, new HtmlTestEncoder());
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public void RenderScriptTags_WithFileVersion()
    {
        // Arrange
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", "/js/site.js"),
                    new TagHelperAttribute("asp-append-version", "true")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

        var helper = GetHelper();
        helper.Src = "/js/site.js";
        helper.AppendVersion = true;

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
    }

    [Theory]
    [InlineData("~/js/site.js", "/js/site.fingerprint.js")]
    [InlineData("/js/site.js", "/js/site.fingerprint.js")]
    [InlineData("js/site.js", "js/site.fingerprint.js")]
    public void RenderScriptTags_WithFileVersion_UsingResourceCollection(string src, string expected)
    {
        // Arrange
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", src),
                    new TagHelperAttribute("asp-append-version", "true")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

        var helper = GetHelper(urlHelperFactory: MakeUrlHelperFactory(value =>
            value.StartsWith("~/", StringComparison.Ordinal) ? value[1..] : value));

        helper.ViewContext.HttpContext.SetEndpoint(CreateEndpoint());
        helper.Src = src;
        helper.AppendVersion = true;

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal(expected, output.Attributes["src"].Value);
    }

    [Theory]
    [InlineData("~/js/site.js")]
    [InlineData("/approot/js/site.js")]
    public void RenderScriptTags_PathBase_WithFileVersion_UsingResourceCollection(string path)
    {
        // Arrange
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", path),
                    new TagHelperAttribute("asp-append-version", "true")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

        var urlHelperFactory = MakeUrlHelperFactory(value =>
        {
            if (value.StartsWith("~/", StringComparison.Ordinal))
            {
                return value.Replace("~/", "/approot/");
            }

            return value;
        });

        var helper = GetHelper(urlHelperFactory: urlHelperFactory);
        helper.ViewContext.HttpContext.SetEndpoint(CreateEndpoint());
        helper.ViewContext.HttpContext.Request.PathBase = "/approot";
        helper.Src = path;
        helper.AppendVersion = true;

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/approot/js/site.fingerprint.js", output.Attributes["src"].Value);
    }

    [Fact]
    public void ScriptTagHelper_RendersProvided_ImportMap()
    {
        // Arrange
        var importMap = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://code.jquery.com/jquery-3.5.1.min.js" },
                { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js" }
            },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["development"] = new Dictionary<string, string>
                {
                    { "jquery", "https://code.jquery.com/jquery-3.5.1.js" },
                    { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js" }
                }.AsReadOnly()
            },
            new Dictionary<string, string>
            {
                { "https://code.jquery.com/jquery-3.5.1.js", "sha384-jquery" },
                { "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js", "sha256-bootstrap" }
            });

        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("type", "importmap"),
                    new TagHelperAttribute("asp-importmap", importMap)
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

        var helper = GetHelper();
        helper.ViewContext.HttpContext.SetEndpoint(CreateEndpoint());
        helper.Type = "importmap";
        helper.ImportMap = importMap;

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal(importMap.ToJson(), output.Content.GetContent());
    }

    [Fact]
    public void ScriptTagHelper_RendersImportMap_FromEndpoint()
    {
        // Arrange
        var importMap = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                { "jquery", "https://code.jquery.com/jquery-3.5.1.min.js" },
                { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js" }
            },
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["development"] = new Dictionary<string, string>
                {
                    { "jquery", "https://code.jquery.com/jquery-3.5.1.js" },
                    { "bootstrap", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js" }
                }.AsReadOnly()
            },
            new Dictionary<string, string>
            {
                { "https://code.jquery.com/jquery-3.5.1.js", "sha384-jquery" },
                { "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.js", "sha256-bootstrap" }
            });

        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("type", "importmap"),
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

        var helper = GetHelper();
        helper.ViewContext.HttpContext.SetEndpoint(CreateEndpoint(importMap));
        helper.Type = "importmap";

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal(importMap.ToJson(), output.Content.GetContent());
    }

    private Endpoint CreateEndpoint(ImportMapDefinition importMap = null)
    {
        return new Endpoint(
            (context) => Task.CompletedTask,
            new EndpointMetadataCollection(
                [new ResourceAssetCollection([
                    new("js/site.fingerprint.js", [new ResourceAssetProperty("label", "js/site.js")]),
                    new("common.fingerprint.js", [new ResourceAssetProperty("label", "common.js")]),
                    new("fallback.fingerprint.js", [new ResourceAssetProperty("label", "fallback.js")]),
                ]),
                importMap ?? new ImportMapDefinition(null, null, null)]),
            "Test");
    }

    [Fact]
    public void RenderScriptTags_WithFileVersion_AndRequestPathBase()
    {
        // Arrange
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", "/bar/js/site.js"),
                    new TagHelperAttribute("asp-append-version", "true")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());
        var viewContext = MakeViewContext("/bar");

        var helper = GetHelper(viewContext: viewContext);
        helper.Src = "/bar/js/site.js";
        helper.AppendVersion = true;

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/bar/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
    }

    [Fact]
    public void RenderScriptTags_FallbackSrc_WithFileVersion()
    {
        // Arrange
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", "/js/site.js"),
                    new TagHelperAttribute("asp-fallback-src-include", "fallback.js"),
                    new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                    new TagHelperAttribute("asp-append-version", "true")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

        var helper = GetHelper();
        helper.FallbackSrc = "fallback.js";
        helper.FallbackTestExpression = "isavailable()";
        helper.AppendVersion = true;
        helper.Src = "/js/site.js";

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
        Assert.Equal(Environment.NewLine + "<script>(isavailable()||document.write(\"JavaScriptEncode[[<script " +
            "src=\"HtmlEncode[[fallback.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\">" +
            "</script>]]\"));</script>", output.PostElement.GetContent());
    }

    [Fact]
    public void RenderScriptTags_FallbackSrc_AppendVersion_WithStaticAssets()
    {
        // Arrange
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", "/js/site.js"),
                    new TagHelperAttribute("asp-fallback-src-include", "fallback.js"),
                    new TagHelperAttribute("asp-fallback-test", "isavailable()"),
                    new TagHelperAttribute("asp-append-version", "true")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());

        var helper = GetHelper();
        helper.ViewContext.HttpContext.SetEndpoint(CreateEndpoint());
        helper.FallbackSrc = "fallback.js";
        helper.FallbackTestExpression = "isavailable()";
        helper.AppendVersion = true;
        helper.Src = "/js/site.js";

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/js/site.fingerprint.js", output.Attributes["src"].Value);
        Assert.Equal(Environment.NewLine + "<script>(isavailable()||document.write(\"JavaScriptEncode[[<script " +
            "src=\"HtmlEncode[[fallback.fingerprint.js]]\">" +
            "</script>]]\"));</script>", output.PostElement.GetContent());
    }

    [Fact]
    public void RenderScriptTags_FallbackSrc_WithFileVersion_EncodesAsExpected()
    {
        // Arrange
        var expectedContent =
            "<script encoded='contains \"quotes\"' literal=\"HtmlEncode[[all HTML encoded]]\" " +
            "mixed='HtmlEncode[[HTML encoded]] and contains \"quotes\"' " +
            "src=\"HtmlEncode[[/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\"></script>" +
            Environment.NewLine +
            "<script>(isavailable()||document.write(\"JavaScriptEncode[[<script encoded=\'contains \"quotes\"\' " +
            "literal=\"HtmlEncode[[all HTML encoded]]\" mixed=\'HtmlEncode[[HTML encoded]] and contains " +
            "\"quotes\"' src=\"HtmlEncode[[fallback.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\">" +
            "</script>]]\"));</script>";
        var mixed = new DefaultTagHelperContent();
        mixed.Append("HTML encoded");
        mixed.AppendHtml(" and contains \"quotes\"");
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    { "asp-append-version", "true" },
                    { "asp-fallback-src-include", "fallback.js" },
                    { "asp-fallback-test", "isavailable()" },
                    { new TagHelperAttribute("encoded", new HtmlString("contains \"quotes\""), HtmlAttributeValueStyle.SingleQuotes) },
                    { "literal", "all HTML encoded" },
                    { new TagHelperAttribute("mixed", mixed, HtmlAttributeValueStyle.SingleQuotes) },
                    { "src", "/js/site.js" },
            });
        var output = MakeTagHelperOutput(
            "script",
            attributes: new TagHelperAttributeList
            {
                    { new TagHelperAttribute("encoded", new HtmlString("contains \"quotes\""), HtmlAttributeValueStyle.SingleQuotes) },
                    { "literal", "all HTML encoded" },
                    { new TagHelperAttribute("mixed", mixed, HtmlAttributeValueStyle.SingleQuotes) },
            });

        var helper = GetHelper();
        helper.AppendVersion = true;
        helper.FallbackSrc = "fallback.js";
        helper.FallbackTestExpression = "isavailable()";
        helper.Src = "/js/site.js";

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
        var content = HtmlContentUtilities.HtmlContentToString(output, new HtmlTestEncoder());
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public void RenderScriptTags_GlobbedSrc_WithFileVersion()
    {
        // Arrange
        var expectedContent = "<script " +
            "src=\"HtmlEncode[[/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\"></script>" +
            "<script src=\"HtmlEncode[[/common.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk]]\"></script>";
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", "/js/site.js"),
                    new TagHelperAttribute("asp-src-include", "*.js"),
                    new TagHelperAttribute("asp-append-version", "true")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());
        var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
            new TestFileProvider(),
            Mock.Of<IMemoryCache>(),
            PathString.Empty);
        globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "*.js", null))
            .Returns(new[] { "/common.js" });

        var helper = GetHelper();
        helper.GlobbingUrlBuilder = globbingUrlBuilder.Object;
        helper.SrcInclude = "*.js";
        helper.AppendVersion = true;
        helper.Src = "/js/site.js";

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/js/site.js?v=f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", output.Attributes["src"].Value);
        var content = HtmlContentUtilities.HtmlContentToString(output, new HtmlTestEncoder());
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public void RenderScriptTags_GlobbedSrc_WithFileVersion_WithStaticAssets()
    {
        // Arrange
        var expectedContent = "<script " +
            "src=\"HtmlEncode[[/js/site.fingerprint.js]]\"></script>" +
            "<script src=\"HtmlEncode[[/common.fingerprint.js]]\"></script>";
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList
            {
                    new TagHelperAttribute("src", "/js/site.js"),
                    new TagHelperAttribute("asp-src-include", "*.js"),
                    new TagHelperAttribute("asp-append-version", "true")
            });
        var output = MakeTagHelperOutput("script", attributes: new TagHelperAttributeList());
        var globbingUrlBuilder = new Mock<GlobbingUrlBuilder>(
            new TestFileProvider(),
            Mock.Of<IMemoryCache>(),
            PathString.Empty);
        globbingUrlBuilder.Setup(g => g.BuildUrlList(null, "*.js", null))
            .Returns(new[] { "/common.js" });

        var helper = GetHelper();
        helper.ViewContext.HttpContext.SetEndpoint(CreateEndpoint());
        helper.GlobbingUrlBuilder = globbingUrlBuilder.Object;
        helper.SrcInclude = "*.js";
        helper.AppendVersion = true;
        helper.Src = "/js/site.js";

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("script", output.TagName);
        Assert.Equal("/js/site.fingerprint.js", output.Attributes["src"].Value);
        var content = HtmlContentUtilities.HtmlContentToString(output, new HtmlTestEncoder());
        Assert.Equal(expectedContent, content);
    }

    private static ScriptTagHelper GetHelper(
        IWebHostEnvironment hostingEnvironment = null,
        IUrlHelperFactory urlHelperFactory = null,
        ViewContext viewContext = null)
    {
        hostingEnvironment = hostingEnvironment ?? MakeHostingEnvironment();
        urlHelperFactory = urlHelperFactory ?? MakeUrlHelperFactory();
        viewContext = viewContext ?? MakeViewContext();

        var memoryCacheProvider = new TagHelperMemoryCacheProvider();
        var fileVersionProvider = new DefaultFileVersionProvider(hostingEnvironment, memoryCacheProvider);

        return new ScriptTagHelper(
            hostingEnvironment,
            memoryCacheProvider,
            fileVersionProvider,
            new HtmlTestEncoder(),
            new JavaScriptTestEncoder(),
            urlHelperFactory)
        {
            ViewContext = viewContext,
        };
    }

    private TagHelperContext MakeTagHelperContext(
        TagHelperAttributeList attributes = null,
        string content = null)
    {
        attributes = attributes ?? new TagHelperAttributeList();

        return new TagHelperContext(
            tagName: "script",
            allAttributes: attributes,
            items: new Dictionary<object, object>(),
            uniqueId: Guid.NewGuid().ToString("N"));
    }

    private static ViewContext MakeViewContext(string requestPathBase = null)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new AspNetCore.Routing.RouteData(), new ActionDescriptor());
        if (requestPathBase != null)
        {
            actionContext.HttpContext.Request.PathBase = new PathString(requestPathBase);
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

    private TagHelperOutput MakeTagHelperOutput(string tagName, TagHelperAttributeList attributes = null)
    {
        attributes = attributes ?? new TagHelperAttributeList();

        return new TagHelperOutput(
            tagName,
            attributes,
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));
    }

    private static IWebHostEnvironment MakeHostingEnvironment()
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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(h => h.WebRootFileProvider).Returns(mockFileProvider.Object);

        return hostingEnvironment.Object;
    }

    private static IUrlHelperFactory MakeUrlHelperFactory(Func<string, string> urlResolver = null)
    {
        var urlHelper = new Mock<IUrlHelper>();
        urlResolver ??= (url) => url;
        urlHelper
            .Setup(helper => helper.Content(It.IsAny<string>()))
            .Returns(new Func<string, string>(urlResolver));

        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelper.Object);

        return urlHelperFactory.Object;
    }
}
