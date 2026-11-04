// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

public class UseMiddlewareTest
{
    [Fact]
    public void UseMiddleware_WithNoParameters_ThrowsException()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseMiddleware(typeof(MiddlewareNoParametersStub)));

        Assert.Equal(
            Resources.FormatException_UseMiddlewareNoParameters(
                UseMiddlewareExtensions.InvokeMethodName,
                UseMiddlewareExtensions.InvokeAsyncMethodName,
                nameof(HttpContext)),
            exception.Message);
    }

    [Fact]
    public void UseMiddleware_AsyncWithNoParameters_ThrowsException()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseMiddleware(typeof(MiddlewareAsyncNoParametersStub)));

        Assert.Equal(
            Resources.FormatException_UseMiddlewareNoParameters(
                UseMiddlewareExtensions.InvokeMethodName,
                UseMiddlewareExtensions.InvokeAsyncMethodName,
                nameof(HttpContext)),
            exception.Message);
    }

    [Fact]
    public void UseMiddleware_NonTaskReturnType_ThrowsException()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseMiddleware(typeof(MiddlewareNonTaskReturnStub)));

        Assert.Equal(
            Resources.FormatException_UseMiddlewareNonTaskReturnType(
                UseMiddlewareExtensions.InvokeMethodName,
                UseMiddlewareExtensions.InvokeAsyncMethodName,
                nameof(Task)),
            exception.Message);
    }

    [Fact]
    public void UseMiddleware_AsyncNonTaskReturnType_ThrowsException()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseMiddleware(typeof(MiddlewareAsyncNonTaskReturnStub)));

        Assert.Equal(
            Resources.FormatException_UseMiddlewareNonTaskReturnType(
                UseMiddlewareExtensions.InvokeMethodName,
                UseMiddlewareExtensions.InvokeAsyncMethodName,
                nameof(Task)),
            exception.Message);
    }

    [Fact]
    public void UseMiddleware_NoInvokeOrInvokeAsyncMethod_ThrowsException()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseMiddleware(typeof(MiddlewareNoInvokeStub)));

        Assert.Equal(
            Resources.FormatException_UseMiddlewareNoInvokeMethod(
                UseMiddlewareExtensions.InvokeMethodName,
                UseMiddlewareExtensions.InvokeAsyncMethodName, typeof(MiddlewareNoInvokeStub)),
            exception.Message);
    }

    [Fact]
    public void UseMiddleware_MultipleInvokeMethods_ThrowsException()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseMiddleware(typeof(MiddlewareMultipleInvokesStub)));

        Assert.Equal(
            Resources.FormatException_UseMiddleMutlipleInvokes(
                UseMiddlewareExtensions.InvokeMethodName,
                UseMiddlewareExtensions.InvokeAsyncMethodName),
            exception.Message);
    }

    [Fact]
    public void UseMiddleware_MultipleInvokeAsyncMethods_ThrowsException()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseMiddleware(typeof(MiddlewareMultipleInvokeAsyncStub)));

        Assert.Equal(
            Resources.FormatException_UseMiddleMutlipleInvokes(
                UseMiddlewareExtensions.InvokeMethodName,
                UseMiddlewareExtensions.InvokeAsyncMethodName),
            exception.Message);
    }

    [Fact]
    public void UseMiddleware_MultipleInvokeAndInvokeAsyncMethods_ThrowsException()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        var exception = Assert.Throws<InvalidOperationException>(() => builder.UseMiddleware(typeof(MiddlewareMultipleInvokeAndInvokeAsyncStub)));

        Assert.Equal(
            Resources.FormatException_UseMiddleMutlipleInvokes(
                UseMiddlewareExtensions.InvokeMethodName,
                UseMiddlewareExtensions.InvokeAsyncMethodName),
            exception.Message);
    }

    [Fact]
    public async Task UseMiddleware_ThrowsIfArgCantBeResolvedFromContainer()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        builder.UseMiddleware(typeof(MiddlewareInjectInvokeNoService));
        var app = builder.Build();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app(new DefaultHttpContext()));
        Assert.Equal(
            Resources.FormatException_InvokeMiddlewareNoService(
                typeof(object),
                typeof(MiddlewareInjectInvokeNoService)),
            exception.Message);
    }

    [Fact]
    public async Task UseMiddleware_ThrowsIfKeyedArgCantBeResolvedFromContainer()
    {
        var builder = new ApplicationBuilder(new DummyKeyedServiceProvider());
        builder.UseMiddleware(typeof(MiddlewareKeyedInjectInvoke));
        var app = builder.Build();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app(new DefaultHttpContext()));
        Assert.Equal(
            Resources.FormatException_InvokeMiddlewareNoService(
                typeof(IKeyedServiceProvider),
                typeof(MiddlewareKeyedInjectInvoke)),
            exception.Message);
    }

    [Fact]
    public void UseMiddleware_ThrowsIfKeyedConstructorArgCantBeResolvedFromContainer()
    {
        var builder = new ApplicationBuilder(new DummyKeyedServiceProvider());
        builder.UseMiddleware(typeof(MiddlewareKeyedConstructorInjectInvoke));
        var exception = Assert.Throws<InvalidOperationException>(builder.Build);
        Assert.Equal(
            $"Unable to resolve service for type '{typeof(IKeyedServiceProvider)}' while attempting to activate '{typeof(MiddlewareKeyedConstructorInjectInvoke)}'.",
            exception.Message);
    }

    [Fact]
    public async Task UseMiddleware_ThrowsIfServiceProviderIsNotAIKeyedServiceProvider()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        builder.UseMiddleware(typeof(MiddlewareKeyedInjectInvoke));
        var app = builder.Build();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app(new DefaultHttpContext()));
        Assert.Equal(
            Resources.Exception_KeyedServicesNotSupported,
            exception.Message);
    }

    [Fact]
    public void UseMiddlewareWithInvokeArg()
    {
        var builder = new ApplicationBuilder(new DummyServiceProvider());
        builder.UseMiddleware(typeof(MiddlewareInjectInvoke));
        var app = builder.Build();
        app(new DefaultHttpContext());
    }

    [Fact]
    public void UseMiddlewareWithInvokeKeyedArg()
    {
        var keyedServiceProvider = new DummyKeyedServiceProvider();
        keyedServiceProvider.AddKeyedService("test", typeof(DummyKeyedServiceProvider), keyedServiceProvider);
        var builder = new ApplicationBuilder(keyedServiceProvider);
        builder.UseMiddleware(typeof(MiddlewareKeyedInjectInvoke));
        var app = builder.Build();
        app(new DefaultHttpContext());
    }

    [Fact]
    public void UseMiddlewareWithConstructorKeyedArg()
    {
        var keyedServiceProvider = new DummyKeyedServiceProvider();
        keyedServiceProvider.AddKeyedService("test", typeof(DummyKeyedServiceProvider), keyedServiceProvider);
        var builder = new ApplicationBuilder(keyedServiceProvider);
        builder.UseMiddleware(typeof(MiddlewareKeyedConstructorInjectInvoke));
        var app = builder.Build();
        app(new DefaultHttpContext());
    }

    [Fact]
    public void UseMiddlewareWithInvokeWithOutAndRefThrows()
    {
        var mockServiceProvider = new DummyServiceProvider();
        var builder = new ApplicationBuilder(mockServiceProvider);
        builder.UseMiddleware(typeof(MiddlewareInjectWithOutAndRefParams));
        var exception = Assert.Throws<NotSupportedException>(() => builder.Build());
    }

    [Fact]
    public void UseMiddlewareWithIMiddlewareThrowsIfParametersSpecified()
    {
        var mockServiceProvider = new DummyServiceProvider();
        var builder = new ApplicationBuilder(mockServiceProvider);
        var exception = Assert.Throws<NotSupportedException>(() => builder.UseMiddleware(typeof(Middleware), "arg"));
        Assert.Equal(Resources.FormatException_UseMiddlewareExplicitArgumentsNotSupported(typeof(IMiddleware)), exception.Message);
    }

    [Fact]
    public async Task UseMiddlewareWithIMiddlewareThrowsIfNoIMiddlewareFactoryRegistered()
    {
        var mockServiceProvider = new DummyServiceProvider();
        var builder = new ApplicationBuilder(mockServiceProvider);
        builder.UseMiddleware(typeof(Middleware));
        var app = builder.Build();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var context = new DefaultHttpContext();
            var sp = new DummyServiceProvider();
            context.RequestServices = sp;
            await app(context);
        });
        Assert.Equal(Resources.FormatException_UseMiddlewareNoMiddlewareFactory(typeof(IMiddlewareFactory)), exception.Message);
    }

    [Fact]
    public async Task UseMiddlewareWithIMiddlewareThrowsIfMiddlewareFactoryCreateReturnsNull()
    {
        var mockServiceProvider = new DummyServiceProvider();
        var builder = new ApplicationBuilder(mockServiceProvider);
        builder.UseMiddleware(typeof(Middleware));
        var app = builder.Build();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var context = new DefaultHttpContext();
            var sp = new DummyServiceProvider();
            sp.AddService(typeof(IMiddlewareFactory), new BadMiddlewareFactory());
            context.RequestServices = sp;
            await app(context);
        });

        Assert.Equal(
            Resources.FormatException_UseMiddlewareUnableToCreateMiddleware(
                typeof(BadMiddlewareFactory),
                typeof(Middleware)),
            exception.Message);
    }

    [Fact]
    public async Task UseMiddlewareWithIMiddlewareWorks()
    {
        var mockServiceProvider = new DummyServiceProvider();
        var builder = new ApplicationBuilder(mockServiceProvider);
        builder.UseMiddleware(typeof(Middleware));
        var app = builder.Build();
        var context = new DefaultHttpContext();
        var sp = new DummyServiceProvider();
        var middlewareFactory = new BasicMiddlewareFactory();
        sp.AddService(typeof(IMiddlewareFactory), middlewareFactory);
        context.RequestServices = sp;
        await app(context);
        Assert.True(Assert.IsType<bool>(context.Items["before"]));
        Assert.True(Assert.IsType<bool>(context.Items["after"]));
        Assert.NotNull(middlewareFactory.Created);
        Assert.NotNull(middlewareFactory.Released);
        Assert.IsType<Middleware>(middlewareFactory.Created);
        Assert.IsType<Middleware>(middlewareFactory.Released);
        Assert.Same(middlewareFactory.Created, middlewareFactory.Released);
    }

    public class Middleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            context.Items["before"] = true;
            await next(context);
            context.Items["after"] = true;
        }
    }

    public class BasicMiddlewareFactory : IMiddlewareFactory
    {
        public IMiddleware? Created { get; private set; }
        public IMiddleware? Released { get; private set; }

        public IMiddleware? Create(Type middlewareType)
        {
            Created = Activator.CreateInstance(middlewareType) as IMiddleware;
            return Created;
        }

        public void Release(IMiddleware middleware)
        {
            Released = middleware;
        }
    }

    public class BadMiddlewareFactory : IMiddlewareFactory
    {
        public IMiddleware? Create(Type middlewareType) => null;

        public void Release(IMiddleware middleware) { }
    }

    private class DummyServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void AddService(Type type, object value) => _services[type] = value;

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (_services.TryGetValue(serviceType, out var value))
            {
                return value;
            }
            return null;
        }
    }

    private class DummyKeyedServiceProvider : IKeyedServiceProvider
    {
        private readonly Dictionary<object, Tuple<Type, object>> _services = new Dictionary<object, Tuple<Type, object>>();

        public DummyKeyedServiceProvider()
        {
                
        }

        public void AddKeyedService(object key, Type type, object value) => _services[key] = new Tuple<Type, object>(type, value);

        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            if (_services.TryGetValue(serviceKey!, out var value))
            {
                return value.Item2;
            }

            return null;
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            var service = GetKeyedService(serviceType, serviceKey);

            if (service == null)
            {
                throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");
            }

            return service;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (_services.TryGetValue(serviceType, out var value))
            {
                return value;
            }
            return null;
        }
    }

    public class MiddlewareInjectWithOutAndRefParams
    {
        public MiddlewareInjectWithOutAndRefParams(RequestDelegate next) { }

        public Task Invoke(HttpContext context, ref IServiceProvider? sp1, out IServiceProvider? sp2)
        {
            sp1 = null;
            sp2 = null;
            return Task.FromResult(0);
        }
    }

    private class MiddlewareInjectInvokeNoService
    {
        public MiddlewareInjectInvokeNoService(RequestDelegate next) { }

        public Task Invoke(HttpContext context, object value) => Task.CompletedTask;
    }

    private class MiddlewareInjectInvoke
    {
        public MiddlewareInjectInvoke(RequestDelegate next) { }

        public Task Invoke(HttpContext context, IServiceProvider provider) => Task.CompletedTask;
    }

    private class MiddlewareKeyedInjectInvoke
    {
        public MiddlewareKeyedInjectInvoke(RequestDelegate next) { }

        public Task Invoke(HttpContext context, [FromKeyedServices("test")] IKeyedServiceProvider provider) => Task.CompletedTask;
    }

    private class MiddlewareKeyedConstructorInjectInvoke
    {
        public MiddlewareKeyedConstructorInjectInvoke(RequestDelegate next, [FromKeyedServices("test")] IKeyedServiceProvider provider) { }

        public Task Invoke(HttpContext context) => Task.CompletedTask;
    }

    private class MiddlewareNoParametersStub
    {
        public MiddlewareNoParametersStub(RequestDelegate next) { }

        public Task Invoke() => Task.CompletedTask;
    }

    private class MiddlewareAsyncNoParametersStub
    {
        public MiddlewareAsyncNoParametersStub(RequestDelegate next) { }

        public Task InvokeAsync() => Task.CompletedTask;
    }

    private class MiddlewareNonTaskReturnStub
    {
        public MiddlewareNonTaskReturnStub(RequestDelegate next) { }

        public int Invoke() => 0;
    }

    private class MiddlewareAsyncNonTaskReturnStub
    {
        public MiddlewareAsyncNonTaskReturnStub(RequestDelegate next) { }

        public int InvokeAsync() => 0;
    }

    private class MiddlewareNoInvokeStub
    {
        public MiddlewareNoInvokeStub(RequestDelegate next) { }
    }

    private class MiddlewareMultipleInvokesStub
    {
        public MiddlewareMultipleInvokesStub(RequestDelegate next) { }

        public Task Invoke(HttpContext context) => Task.CompletedTask;

        public Task Invoke(HttpContext context, int i) => Task.CompletedTask;
    }

    private class MiddlewareMultipleInvokeAsyncStub
    {
        public MiddlewareMultipleInvokeAsyncStub(RequestDelegate next) { }

        public Task InvokeAsync(HttpContext context) => Task.CompletedTask;

        public Task InvokeAsync(HttpContext context, int i) => Task.CompletedTask;
    }

    private class MiddlewareMultipleInvokeAndInvokeAsyncStub
    {
        public MiddlewareMultipleInvokeAndInvokeAsyncStub(RequestDelegate next) { }

        public Task Invoke(HttpContext context) => Task.CompletedTask;

        public Task InvokeAsync(HttpContext context) => Task.CompletedTask;
    }
}
