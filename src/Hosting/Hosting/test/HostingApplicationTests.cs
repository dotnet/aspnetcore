using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static Microsoft.AspNetCore.Hosting.HostingApplication;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class HostingApplicationTests
    {
        [Fact]
        public void DisposeContextDoesNotClearHttpContextIfDefaultHttpContextFactoryUsed()
        {
            // Arrange
            var hostingApplication = CreateApplication();
            var httpContext = new DefaultHttpContext();

            var context = hostingApplication.CreateContext(httpContext.Features);
            Assert.NotNull(context.HttpContext);

            // Act/Assert
            hostingApplication.DisposeContext(context, null);
            Assert.NotNull(context.HttpContext);
        }

        [Fact]
        public void DisposeContextClearsHttpContextIfIHttpContextAccessorIsActive()
        {
            // Arrange
            var hostingApplication = CreateApplication(useHttpContextAccessor: true);
            var httpContext = new DefaultHttpContext();

            var context = hostingApplication.CreateContext(httpContext.Features);
            Assert.NotNull(context.HttpContext);

            // Act/Assert
            hostingApplication.DisposeContext(context, null);
            Assert.Null(context.HttpContext);
        }

        [Fact]
        public void CreateContextReinitializesPreviouslyStoredDefaultHttpContext()
        {
            // Arrange
            var hostingApplication = CreateApplication();
            var features = new FeaturesWithContext<Context>(new DefaultHttpContext().Features);
            var previousContext = new DefaultHttpContext();
            // Pretend like we had previous HttpContext
            features.HostContext = new Context();
            features.HostContext.HttpContext = previousContext;

            var context = hostingApplication.CreateContext(features);
            Assert.Same(previousContext, context.HttpContext);

            // Act/Assert
            hostingApplication.DisposeContext(context, null);
            Assert.Same(previousContext, context.HttpContext);
        }

        [Fact]
        public void CreateContextCreatesNewContextIfNotUsingDefaultHttpContextFactory()
        {
            // Arrange
            var factory = new Mock<IHttpContextFactory>();
            factory.Setup(m => m.Create(It.IsAny<IFeatureCollection>())).Returns<IFeatureCollection>(f => new DefaultHttpContext(f));
            factory.Setup(m => m.Dispose(It.IsAny<HttpContext>())).Callback(() => { });

            var hostingApplication = CreateApplication(factory.Object);
            var features = new FeaturesWithContext<Context>(new DefaultHttpContext().Features);
            var previousContext = new DefaultHttpContext();
            // Pretend like we had previous HttpContext
            features.HostContext = new Context();
            features.HostContext.HttpContext = previousContext;

            var context = hostingApplication.CreateContext(features);
            Assert.NotSame(previousContext, context.HttpContext);

            // Act/Assert
            hostingApplication.DisposeContext(context, null);
        }

        private static HostingApplication CreateApplication(IHttpContextFactory httpContextFactory = null, bool useHttpContextAccessor = false)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            if (useHttpContextAccessor)
            {
                services.AddHttpContextAccessor();
            }

            httpContextFactory ??= new DefaultHttpContextFactory(services.BuildServiceProvider());

            var hostingApplication = new HostingApplication(
                ctx => Task.CompletedTask,
                NullLogger.Instance,
                new DiagnosticListener("Microsoft.AspNetCore"),
                httpContextFactory);

            return hostingApplication;
        }

        private class FeaturesWithContext<T> : IHostContextContainer<T>, IFeatureCollection
        {
            public FeaturesWithContext(IFeatureCollection features)
            {
                Features = features;
            }

            public IFeatureCollection Features { get; }

            public object this[Type key] { get => Features[key]; set => Features[key] = value; }

            public T HostContext { get; set; }

            public bool IsReadOnly => Features.IsReadOnly;

            public int Revision => Features.Revision;

            public TFeature Get<TFeature>() => Features.Get<TFeature>();

            public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => Features.GetEnumerator();

            public void Set<TFeature>(TFeature instance) => Features.Set(instance);

            IEnumerator IEnumerable.GetEnumerator() => Features.GetEnumerator();
        }
    }
}
