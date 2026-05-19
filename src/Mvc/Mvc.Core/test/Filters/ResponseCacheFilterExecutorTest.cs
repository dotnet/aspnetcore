// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class ResponseCacheFilterExecutorTest
{
    [Fact]
    public void Execute_DoesNotThrow_WhenNoStoreIsTrue()
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile
            {
                NoStore = true,
                Duration = null
            });
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal("no-store", context.HttpContext.Response.Headers["Cache-control"]);
    }

    [Fact]
    public void Execute_DoesNotThrowIfDurationIsNotSet_WhenNoStoreIsFalse()
    {
        // Arrange, Act
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile
            {
                Duration = null
            });

        // Assert
        Assert.NotNull(executor);
    }

    [Fact]
    public void Execute_ThrowsIfDurationIsNotSet_WhenNoStoreIsFalse()
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile()
            {
                Duration = null
            });

        var context = GetActionExecutingContext();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => executor.Execute(context));
        Assert.Equal("If the 'NoStore' property is not set to true, 'Duration' property must be specified.",
            ex.Message);
    }

    public static TheoryData<CacheProfile, string> CacheControlData
    {
        get
        {
            return new TheoryData<CacheProfile, string>
                {
                    {
                        new CacheProfile
                        {
                            Duration = 0,
                            Location = ResponseCacheLocation.Any,
                            NoStore = true,
                            VaryByHeader = null
                        },
                        "no-store"
                    },
                    // If no-store is set, then location is ignored.
                    {
                        new CacheProfile
                        {
                            Duration = 0,
                            Location = ResponseCacheLocation.Client,
                            NoStore = true,
                            VaryByHeader = null
                        },
                        "no-store"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 0,
                            Location = ResponseCacheLocation.Any,
                            NoStore = true,
                            VaryByHeader = null
                        },
                        "no-store"
                    },
                    // If no-store is set, then duration is ignored.
                    {
                        new CacheProfile
                        {
                            Duration = 100,
                            Location = ResponseCacheLocation.Any,
                            NoStore = true,
                            VaryByHeader = null
                        },
                        "no-store"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Client,
                            NoStore = false,
                            VaryByHeader = null
                        },
                        "private,max-age=10"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Any,
                            NoStore = false,
                            VaryByHeader = null
                        },
                        "public,max-age=10"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.None,
                            NoStore = false,
                            VaryByHeader = null
                        },
                        "no-cache,max-age=10"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = null,
                            Location = ResponseCacheLocation.None,
                            NoStore = false,
                            VaryByHeader = null
                        },
                        "no-cache"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 31536000,
                            Location = ResponseCacheLocation.Any,
                            NoStore = false,
                            VaryByHeader = null
                        },
                        "public,max-age=31536000"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 20,
                            Location = ResponseCacheLocation.Any,
                            NoStore = false,
                            VaryByHeader = null
                        },
                        "public,max-age=20"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(CacheControlData))]
    public void Execute_CanSetCacheControlHeaders(CacheProfile cacheProfile, string output)
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(cacheProfile);
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal(output, context.HttpContext.Response.Headers["Cache-control"]);
    }

    public static TheoryData<CacheProfile, string> NoStoreData
    {
        get
        {
            return new TheoryData<CacheProfile, string>
                {
                    // If no-store is set, then location is ignored.
                    {
                        new CacheProfile
                        {
                            Duration = 0,
                            Location = ResponseCacheLocation.Client,
                            NoStore = true,
                            VaryByHeader = null
                        },
                        "no-store"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 0,
                            Location = ResponseCacheLocation.Any,
                            NoStore = true,
                            VaryByHeader = null
                        },
                        "no-store"
                    },
                    // If no-store is set, then duration is ignored.
                    {
                        new CacheProfile
                        {
                            Duration = 100,
                            Location = ResponseCacheLocation.Any,
                            NoStore = true,
                            VaryByHeader = null
                        },
                        "no-store"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(NoStoreData))]
    public void Execute_DoesNotSetLocationOrDuration_IfNoStoreIsSet(CacheProfile cacheProfile, string output)
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(cacheProfile);
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal(output, context.HttpContext.Response.Headers["Cache-control"]);
    }

    public static TheoryData<CacheProfile, string, string> VaryByHeaderData
    {
        get
        {
            return new TheoryData<CacheProfile, string, string>
                {
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Any,
                            NoStore = false,
                            VaryByHeader = "Accept"
                        },
                        "Accept",
                        "public,max-age=10"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 0,
                            Location = ResponseCacheLocation.Any,
                            NoStore = true,
                            VaryByHeader = "Accept"
                        },
                        "Accept",
                        "no-store"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Client,
                            NoStore = false,
                            VaryByHeader = "Accept"
                        },
                        "Accept",
                        "private,max-age=10"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Client,
                            NoStore = false,
                            VaryByHeader = "Test"
                        },
                        "Test",
                        "private,max-age=10"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 31536000,
                            Location = ResponseCacheLocation.Any,
                            NoStore = false,
                            VaryByHeader = "Test"
                        },
                        "Test",
                        "public,max-age=31536000"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(VaryByHeaderData))]
    public void ResponseCacheCanSetVaryByHeader(CacheProfile cacheProfile, string varyOutput, string cacheControlOutput)
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(cacheProfile);
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal(varyOutput, context.HttpContext.Response.Headers["Vary"]);
        Assert.Equal(cacheControlOutput, context.HttpContext.Response.Headers["Cache-control"]);
    }

    public static TheoryData<CacheProfile, string[], string> VaryByQueryKeyData
    {
        get
        {
            return new TheoryData<CacheProfile, string[], string>
                {
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Any,
                            NoStore = false,
                            VaryByQueryKeys = new[] { "Accept" }
                        },
                        new[] { "Accept" },
                        "public,max-age=10"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 0,
                            Location = ResponseCacheLocation.Any,
                            NoStore = true,
                            VaryByQueryKeys = new[] { "Accept" }
                        },
                        new[] { "Accept" },
                        "no-store"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Client,
                            NoStore = false,
                            VaryByQueryKeys = new[] { "Accept" }
                        },
                        new[] { "Accept" },
                        "private,max-age=10"
                    },
                    {
                        new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Client,
                            NoStore = false,
                            VaryByQueryKeys = new[] { "Accept", "Test" }
                        },
                        new[] { "Accept", "Test" },
                        "private,max-age=10"
                    },
                    {

                        new CacheProfile
                        {
                            Duration = 31536000,
                            Location = ResponseCacheLocation.Any,
                            NoStore = false,
                            VaryByQueryKeys = new[] { "Accept", "Test" }
                        },
                        new[] { "Accept", "Test" },
                        "public,max-age=31536000"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(VaryByQueryKeyData))]
    public void ResponseCacheCanSetVaryByQueryKeys(CacheProfile cacheProfile, string[] varyOutput, string cacheControlOutput)
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(cacheProfile);
        var context = GetActionExecutingContext();
        context.HttpContext.Features.Set<IResponseCachingFeature>(new ResponseCachingFeature());

        // Acts
        executor.Execute(context);

        // Assert
        Assert.Equal(varyOutput, context.HttpContext.Features.Get<IResponseCachingFeature>().VaryByQueryKeys);
        Assert.Equal(cacheControlOutput, context.HttpContext.Response.Headers.CacheControl);
    }

    [Fact]
    public void NonEmptyVaryByQueryKeys_WithoutConfiguringMiddleware_Throws()
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile
            {
                Duration = 0,
                Location = ResponseCacheLocation.None,
                NoStore = true,
                VaryByHeader = null,
                VaryByQueryKeys = new[] { "Test" }
            });
        var context = GetActionExecutingContext();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => executor.Execute(context));
        Assert.Equal("'VaryByQueryKeys' requires the response cache middleware.", exception.Message);
    }

    [Fact]
    public void SetsPragmaOnNoCache()
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile
            {
                Duration = 0,
                Location = ResponseCacheLocation.None,
                NoStore = true,
                VaryByHeader = null
            });
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal("no-store,no-cache", context.HttpContext.Response.Headers["Cache-control"]);
        Assert.Equal("no-cache", context.HttpContext.Response.Headers["Pragma"]);
    }

    [Fact]
    public void FilterDurationProperty_OverridesCachePolicySetting()
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile
            {
                Duration = 10
            });
        executor.Duration = 20;
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal("public,max-age=20", context.HttpContext.Response.Headers["Cache-control"]);
    }

    [Fact]
    public void FilterLocationProperty_OverridesCachePolicySetting()
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile
            {
                Duration = 10,
                Location = ResponseCacheLocation.None
            });
        executor.Location = ResponseCacheLocation.Client;
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal("private,max-age=10", context.HttpContext.Response.Headers["Cache-control"]);
    }

    [Fact]
    public void FilterNoStoreProperty_OverridesCachePolicySetting()
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile
            {
                NoStore = true
            });
        executor.NoStore = false;
        executor.Duration = 10;
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal("public,max-age=10", context.HttpContext.Response.Headers["Cache-control"]);
    }

    [Fact]
    public void FilterVaryByProperty_OverridesCachePolicySetting()
    {
        // Arrange
        var executor = new ResponseCacheFilterExecutor(
            new CacheProfile
            {
                NoStore = true,
                VaryByHeader = "Accept"
            });
        executor.VaryByHeader = "Test";
        var context = GetActionExecutingContext();

        // Act
        executor.Execute(context);

        // Assert
        Assert.Equal("Test", context.HttpContext.Response.Headers["Vary"]);
    }

    private ActionExecutingContext GetActionExecutingContext(List<IFilterMetadata> filters = null)
    {
        return new ActionExecutingContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            filters ?? new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());
    }
}
