// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingMiddlewareTests
{
    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullArgs()
    {
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>();

        Assert.Throws<ArgumentNullException>(() => new RateLimitingMiddleware(
            null,
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>()));

        Assert.Throws<ArgumentNullException>(() => new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            null,
            options,
            Mock.Of<IServiceProvider>()));

        Assert.Throws<ArgumentNullException>(() => new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            null));
    }

    [Fact]
    public async Task RequestsCallNextIfAccepted()
    {
        var flag = false;
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(true));
        var middleware = new RateLimitingMiddleware(c =>
            {
                flag = true;
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        await middleware.Invoke(new DefaultHttpContext());
        Assert.True(flag);
    }

    [Fact]
    public async Task RequestRejected_CallsOnRejectedAndGives503()
    {
        var onRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
        options.Value.OnRejected = (context, token) =>
        {
            onRejectedInvoked = true;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(onRejectedInvoked);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task RequestRejected_WinsOverDefaultStatusCode()
    {
        var onRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
        options.Value.OnRejected = (context, token) =>
        {
            onRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(onRejectedInvoked);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task RequestAborted_DoesNotThrowTaskCanceledException()
    {
        var sink = new TestSink(
            TestSink.EnableWithTypeName<RateLimitingMiddleware>,
            TestSink.EnableWithTypeName<RateLimitingMiddleware>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);

        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            loggerFactory.CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.RequestAborted = new CancellationToken(true);
        await middleware.Invoke(context);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        var logMessages = sink.Writes.ToList();

        Assert.Single(logMessages);
        var message = logMessages.First();
        Assert.Equal(LogLevel.Debug, message.LogLevel);
        Assert.Equal("The request was canceled.", message.State.ToString());
    }

    [Fact]
    public async Task EndpointLimiterRequested_NoPolicy_Throws()
    {
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context)).DefaultTimeout();
    }

    [Fact]
    public async Task EndpointLimiter_Rejects()
    {
        var onRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";
        options.Value.AddPolicy<string>(name, (context =>
        {
            return RateLimitPartition.Get<string>("myLimiter", (key =>
            {
                return new TestRateLimiter(false);
            }));
        }));
        options.Value.OnRejected = (context, token) =>
        {
            onRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(onRejectedInvoked);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task EndpointLimiterConvenienceMethod_Rejects()
    {
        var onRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";
        options.Value.AddFixedWindowLimiter(name, options =>
        {
            options.PermitLimit = 1;
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 0;
            options.Window = TimeSpan.FromSeconds(10);
            options.AutoReplenishment = false;
        });
        options.Value.OnRejected = (context, token) =>
        {
            onRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(onRejectedInvoked);
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(onRejectedInvoked);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task EndpointLimiterRejects_EndpointOnRejectedFires()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";
        // This is the policy that should get used
        options.Value.AddPolicy<string>(name, new TestRateLimiterPolicy("myKey", 404, false));
        // This OnRejected should be ignored in favor of the one on the policy
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task GlobalAndEndpoint_GlobalRejects_GlobalWins()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";
        // Endpoint always allows - it should not fire
        options.Value.AddPolicy<string>(name, new TestRateLimiterPolicy("myKey", 404, true));
        // Global never allows - it should fire
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task GlobalAndEndpoint_EndpointRejects_EndpointWins()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";
        // Endpoint never allows - it should fire
        options.Value.AddPolicy<string>(name, new TestRateLimiterPolicy("myKey", 404, false));
        // Global always allows - it should not fire
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(true));
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task GlobalAndEndpoint_BothReject_GlobalWins()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";
        // Endpoint never allows - it should not fire
        options.Value.AddPolicy<string>(name, new TestRateLimiterPolicy("myKey", 404, false));
        // Global never allows - it should fire
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task EndpointLimiterRejects_EndpointOnRejectedFires_WithIRateLimiterPolicy()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";
        // This is the policy that should get used
        options.Value.AddPolicy<string, TestRateLimiterPolicy>(name);
        // This OnRejected should be ignored in favor of the one on the policy
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        // Configure the service provider with the args to the TestRateLimiterPolicy ctor
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(string)))
            .Returns("myKey");
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(int)))
            .Returns(404);
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(bool)))
            .Returns(false);

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            mockServiceProvider.Object);

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task EndpointLimiter_DuplicatePartitionKey_NoCollision()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var endpointName1 = "myEndpoint1";
        var endpointName2 = "myEndpoint2";
        var duplicateKey = "myKey";
        // Two policies with the same partition key should not collide, because DefaultKeyType has reference equality
        options.Value.AddPolicy<string>(endpointName1, new TestRateLimiterPolicy(duplicateKey, 404, false));
        options.Value.AddPolicy<string>(endpointName2, new TestRateLimiterPolicy(duplicateKey, 400, false));
        // This OnRejected should be ignored in favor of the ones on the policy
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        var endpoint1 = new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(endpointName1)), "Test endpoint 1");
        var endpoint2 = new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(endpointName2)), "Test endpoint 2");

        context.SetEndpoint(endpoint1);
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);
        // This should hit endpointName1
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        context.SetEndpoint(endpoint2);
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);
        // This should hit endpointName2
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task EndpointLimiter_DuplicatePartitionKey_Lambda_NoCollision()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var endpointName1 = "myEndpoint1";
        var endpointName2 = "myEndpoint2";
        var duplicateKey = "myKey";
        // Two policies with the same partition key should not collide, because DefaultKeyType has reference equality
        options.Value.AddPolicy<string>(endpointName1, key =>
        {
            return new RateLimitPartition<string>(duplicateKey, partitionKey =>
            {
                return new TestRateLimiter(false);
            });
        });
        options.Value.AddPolicy<string>(endpointName2, key =>
        {
            return new RateLimitPartition<string>(duplicateKey, partitionKey =>
            {
                return new TestRateLimiter(true);
            });
        });
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        var endpoint1 = new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(endpointName1)), "Test endpoint 1");
        var endpoint2 = new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(endpointName2)), "Test endpoint 2");

        context.SetEndpoint(endpoint1);
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(globalOnRejectedInvoked);
        // This should hit endpointName1
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);

        globalOnRejectedInvoked = false;

        context.SetEndpoint(endpoint2);
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);
    }

    [Fact]
    public async Task DisableRateLimitingAttribute_SkipsGlobalAndEndpoint()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        var name = "myEndpoint";
        // Endpoint never allows
        options.Value.AddPolicy<string>(name, new TestRateLimiterPolicy("myKey", 404, false));
        // Global never allows
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        // DisableRateLimitingAttribute last
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(name), new DisableRateLimitingAttribute()), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        // DisableRateLimitingAttribute first
        context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new DisableRateLimitingAttribute(), new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task PolicyDirectlyOnEndpoint_GetsUsed()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        // Policy will disallow
        var policy = new TestRateLimiterPolicy("myKey", 404, false);
        var defaultRateLimiterPolicy = new DefaultRateLimiterPolicy(RateLimiterOptions.ConvertPartitioner<string>(null, policy.GetPartition), policy.OnRejected);
        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(defaultRateLimiterPolicy)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task MultipleEndpointPolicies_LastOneWins()
    {
        var globalOnRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        // Policy will disallow
        var policy = new TestRateLimiterPolicy("myKey1", 404, false);
        var defaultRateLimiterPolicy = new DefaultRateLimiterPolicy(RateLimiterOptions.ConvertPartitioner<string>(null, policy.GetPartition), policy.OnRejected);

        var name = "myEndpoint";
        options.Value.AddPolicy<string>(name, new TestRateLimiterPolicy("myKey2", 403, false));

        options.Value.OnRejected = (context, token) =>
        {
            globalOnRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(defaultRateLimiterPolicy), new EnableRateLimitingAttribute(name)), "Test endpoint"));
        await middleware.Invoke(context).DefaultTimeout();
        Assert.False(globalOnRejectedInvoked);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    private IOptions<RateLimiterOptions> CreateOptionsAccessor() => Options.Create(new RateLimiterOptions());

}
