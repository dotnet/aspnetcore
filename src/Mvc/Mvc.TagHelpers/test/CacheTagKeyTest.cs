// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class CacheTagKeyTest
{
    [Fact]
    public void GenerateKey_ReturnsKeyBasedOnTagHelperUniqueId()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var tagHelperContext = GetTagHelperContext(id);
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };
        var expected = "CacheTagHelper||" + id;

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void Equals_ReturnsTrueOnSameKey()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var tagHelperContext1 = GetTagHelperContext(id);
        var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };

        var tagHelperContext2 = GetTagHelperContext(id);
        var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };

        // Act
        var cacheTagKey1 = new CacheTagKey(cacheTagHelper1, tagHelperContext1);
        var cacheTagKey2 = new CacheTagKey(cacheTagHelper2, tagHelperContext2);

        // Assert
        Assert.Equal(cacheTagKey1, cacheTagKey2);
    }

    [Fact]
    public void Equals_ReturnsFalseOnDifferentKey()
    {
        // Arrange
        var tagHelperContext1 = GetTagHelperContext("some-id");
        var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };

        var tagHelperContext2 = GetTagHelperContext("some-other-id");
        var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };

        // Act
        var cacheTagKey1 = new CacheTagKey(cacheTagHelper1, tagHelperContext1);
        var cacheTagKey2 = new CacheTagKey(cacheTagHelper2, tagHelperContext2);

        // Assert
        Assert.NotEqual(cacheTagKey1, cacheTagKey2);
    }

    [Fact]
    public void GetHashCode_IsSameForSimilarCacheTagHelper()
    {
        // Arrange
        var tagHelperContext1 = GetTagHelperContext("some-id");
        var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };

        var tagHelperContext2 = GetTagHelperContext("some-id");
        var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };

        var cacheKey1 = new CacheTagKey(cacheTagHelper1, tagHelperContext1);
        var cacheKey2 = new CacheTagKey(cacheTagHelper2, tagHelperContext2);

        // Act
        var hashcode1 = cacheKey1.GetHashCode();
        var hashcode2 = cacheKey2.GetHashCode();

        // Assert
        Assert.Equal(hashcode1, hashcode2);
    }

    [Fact]
    public void GetHashCode_VariesByUniqueId()
    {
        // Arrange
        var tagHelperContext1 = GetTagHelperContext("some-id");
        var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };

        var tagHelperContext2 = GetTagHelperContext("some-other-id");
        var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext()
        };

        var cacheKey1 = new CacheTagKey(cacheTagHelper1, tagHelperContext1);
        var cacheKey2 = new CacheTagKey(cacheTagHelper2, tagHelperContext2);

        // Act
        var hashcode1 = cacheKey1.GetHashCode();
        var hashcode2 = cacheKey2.GetHashCode();

        // Assert
        Assert.NotEqual(hashcode1, hashcode2);
    }

    [Fact]
    public void GenerateKey_ReturnsKeyBasedOnTagHelperName()
    {
        // Arrange
        var name = "some-name";
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new DistributedCacheTagHelper(
            Mock.Of<IDistributedCacheTagHelperService>(),
            new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            Name = name
        };
        var expected = "DistributedCacheTagHelper||" + name;

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("Vary-By-Value")]
    [InlineData("Vary  with spaces")]
    [InlineData("  Vary  with more spaces   ")]
    public void GenerateKey_UsesVaryByPropertyToGenerateKey(string varyBy)
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryBy = varyBy
        };
        var expected = "CacheTagHelper||testid||VaryBy||" + varyBy;

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("Cookie0", "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value)")]
    [InlineData("Cookie0,Cookie1",
        "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value||Cookie1||Cookie1Value)")]
    [InlineData("Cookie0, Cookie1",
        "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value||Cookie1||Cookie1Value)")]
    [InlineData("   Cookie0,   ,   Cookie1   ",
        "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value||Cookie1||Cookie1Value)")]
    [InlineData(",Cookie0,,Cookie1,",
        "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value||Cookie1||Cookie1Value)")]
    public void GenerateKey_UsesVaryByCookieName(string varyByCookie, string expected)
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByCookie = varyByCookie
        };
        cacheTagHelper.ViewContext.HttpContext.Request.Headers["Cookie"] =
            "Cookie0=Cookie0Value;Cookie1=Cookie1Value";

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("Accept-Language", "CacheTagHelper||testid||VaryByHeader(Accept-Language||en-us;charset=utf8)")]
    [InlineData("X-CustomHeader,Accept-Encoding, NotAvailable",
        "CacheTagHelper||testid||VaryByHeader(X-CustomHeader||Header-Value||Accept-Encoding||utf8||NotAvailable||)")]
    [InlineData("X-CustomHeader,  , Accept-Encoding, NotAvailable",
        "CacheTagHelper||testid||VaryByHeader(X-CustomHeader||Header-Value||Accept-Encoding||utf8||NotAvailable||)")]
    public void GenerateKey_UsesVaryByHeader(string varyByHeader, string expected)
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByHeader = varyByHeader
        };
        var headers = cacheTagHelper.ViewContext.HttpContext.Request.Headers;
        headers["Accept-Language"] = "en-us;charset=utf8";
        headers["Accept-Encoding"] = "utf8";
        headers["X-CustomHeader"] = "Header-Value";

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("category", "CacheTagHelper||testid||VaryByQuery(category||cats)")]
    [InlineData("Category,SortOrder,SortOption",
        "CacheTagHelper||testid||VaryByQuery(Category||cats||SortOrder||||SortOption||Adorability)")]
    [InlineData("Category,  SortOrder, SortOption,  ",
        "CacheTagHelper||testid||VaryByQuery(Category||cats||SortOrder||||SortOption||Adorability)")]
    public void GenerateKey_UsesVaryByQuery(string varyByQuery, string expected)
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByQuery = varyByQuery
        };
        cacheTagHelper.ViewContext.HttpContext.Request.QueryString =
            new QueryString("?sortoption=Adorability&Category=cats&sortOrder=");

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("id", "CacheTagHelper||testid||VaryByRoute(id||4)")]
    [InlineData("Category,,Id,OptionRouteValue",
        "CacheTagHelper||testid||VaryByRoute(Category||MyCategory||Id||4||OptionRouteValue||)")]
    [InlineData(" Category,  , Id,   OptionRouteValue,   ",
        "CacheTagHelper||testid||VaryByRoute(Category||MyCategory||Id||4||OptionRouteValue||)")]
    public void GenerateKey_UsesVaryByRoute(string varyByRoute, string expected)
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByRoute = varyByRoute
        };
        cacheTagHelper.ViewContext.RouteData.Values["id"] = 4;
        cacheTagHelper.ViewContext.RouteData.Values["category"] = "MyCategory";

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    [ReplaceCulture("de-CH", "de-CH")]
    public void GenerateKey_UsesVaryByRoute_UsesInvariantCulture()
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(
            new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByRoute = "Category",
        };
        cacheTagHelper.ViewContext.RouteData.Values["id"] = 4;
        cacheTagHelper.ViewContext.RouteData.Values["category"] =
            new DateTimeOffset(2018, 10, 31, 7, 37, 38, TimeSpan.FromHours(-7));
        var expected = "CacheTagHelper||testid||VaryByRoute(Category||10/31/2018 07:37:38 -07:00)";

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void GenerateKey_UsesVaryByUser_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var expected = "CacheTagHelper||testid||VaryByUser||";
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByUser = true
        };

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void GenerateKey_UsesVaryByUserAndAuthenticatedUserName()
    {
        // Arrange
        var expected = "CacheTagHelper||testid||VaryByUser||test_name";
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByUser = true
        };
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimsIdentity.DefaultNameClaimType, "test_name") });
        cacheTagHelper.ViewContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    [ReplaceCulture("fr-FR", "es-ES")]
    public void GenerateKey_UsesCultureAndUICultureName_IfVaryByCulture_IsSet()
    {
        // Arrange
        var expected = "CacheTagHelper||testid||VaryByCulture||fr-FR||es-ES";
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByCulture = true
        };

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void GenerateKey_WithMultipleVaryByOptions_CreatesCombinedKey()
    {
        // Arrange
        var expected = "CacheTagHelper||testid||VaryBy||custom-value||" +
            "VaryByHeader(content-type||text/html)||VaryByUser||someuser";
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByUser = true,
            VaryByHeader = "content-type",
            VaryBy = "custom-value"
        };
        cacheTagHelper.ViewContext.HttpContext.Request.Headers["Content-Type"] = "text/html";
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimsIdentity.DefaultNameClaimType, "someuser") });
        cacheTagHelper.ViewContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    [ReplaceCulture("zh", "zh-Hans")]
    public void GenerateKey_WithVaryByCulture_ComposesWithOtherOptions()
    {
        // Arrange
        var expected = "CacheTagHelper||testid||VaryBy||custom-value||" +
            "VaryByHeader(content-type||text/html)||VaryByCulture||zh||zh-Hans";
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByCulture = true,
            VaryByHeader = "content-type",
            VaryBy = "custom-value"
        };
        cacheTagHelper.ViewContext.HttpContext.Request.Headers["Content-Type"] = "text/html";

        // Act
        var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
        var key = cacheTagKey.GenerateKey();

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void Equality_ReturnsFalse_WhenVaryByCultureIsTrue_AndCultureIsDifferent()
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByCulture = true,
        };

        // Act
        CacheTagKey key1;
        using (new CultureReplacer("fr-FR"))
        {
            key1 = new CacheTagKey(cacheTagHelper, tagHelperContext);
        }

        CacheTagKey key2;
        using (new CultureReplacer("es-ES"))
        {
            key2 = new CacheTagKey(cacheTagHelper, tagHelperContext);
        }
        var equals = key1.Equals(key2);
        var hashCode1 = key1.GetHashCode();
        var hashCode2 = key2.GetHashCode();

        // Assert
        Assert.False(equals, "CacheTagKeys must not be equal");
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void Equality_ReturnsFalse_WhenVaryByCultureIsTrue_AndUICultureIsDifferent()
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByCulture = true,
        };

        // Act
        CacheTagKey key1;
        using (new CultureReplacer("fr", "fr-FR"))
        {
            key1 = new CacheTagKey(cacheTagHelper, tagHelperContext);
        }

        CacheTagKey key2;
        using (new CultureReplacer("fr", "fr-CA"))
        {
            key2 = new CacheTagKey(cacheTagHelper, tagHelperContext);
        }
        var equals = key1.Equals(key2);
        var hashCode1 = key1.GetHashCode();
        var hashCode2 = key2.GetHashCode();

        // Assert
        Assert.False(equals, "CacheTagKeys must not be equal");
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void Equality_ReturnsTrue_WhenVaryByCultureIsTrue_AndCultureIsSame()
    {
        // Arrange
        var tagHelperContext = GetTagHelperContext();
        var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(Mock.Of<IMemoryCache>()), new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            VaryByCulture = true,
        };

        // Act
        CacheTagKey key1;
        CacheTagKey key2;
        using (new CultureReplacer("fr-FR", "fr-FR"))
        {
            key1 = new CacheTagKey(cacheTagHelper, tagHelperContext);
        }

        using (new CultureReplacer("fr-fr", "fr-fr"))
        {
            key2 = new CacheTagKey(cacheTagHelper, tagHelperContext);
        }

        var equals = key1.Equals(key2);
        var hashCode1 = key1.GetHashCode();
        var hashCode2 = key2.GetHashCode();

        // Assert
        Assert.True(equals, "CacheTagKeys must be equal");
        Assert.Equal(hashCode1, hashCode2);
    }

    private static ViewContext GetViewContext()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ViewContext(actionContext,
            Mock.Of<IView>(),
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            Mock.Of<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());
    }

    private static TagHelperContext GetTagHelperContext(string id = "testid")
    {
        return new TagHelperContext(
            tagName: "test",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: id);
    }
}
