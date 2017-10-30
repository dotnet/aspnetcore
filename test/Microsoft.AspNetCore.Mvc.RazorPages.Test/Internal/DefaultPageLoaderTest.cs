// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageLoaderTest
    {
        [Fact]
        public void Load_InvokesApplicationModelProviders()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();

            var compilerProvider = GetCompilerProvider();

            var razorPagesOptions = Options.Create(new RazorPagesOptions());
            var mvcOptions = Options.Create(new MvcOptions());

            var provider1 = new Mock<IPageApplicationModelProvider>();
            var provider2 = new Mock<IPageApplicationModelProvider>();

            var sequence = 0;
            var pageApplicationModel1 = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), new object[0]);
            var pageApplicationModel2 = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), new object[0]);

            provider1.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(0, sequence++);
                    Assert.Null(c.PageApplicationModel);
                    c.PageApplicationModel = pageApplicationModel1;
                })
                .Verifiable();

            provider2.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(1, sequence++);
                    Assert.Same(pageApplicationModel1, c.PageApplicationModel);
                    c.PageApplicationModel = pageApplicationModel2;
                })
                .Verifiable();

            provider1.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(3, sequence++);
                    Assert.Same(pageApplicationModel2, c.PageApplicationModel);
                })
                .Verifiable();

            provider2.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(2, sequence++);
                    Assert.Same(pageApplicationModel2, c.PageApplicationModel);
                })
                .Verifiable();

            var providers = new[]
            {
                provider1.Object, provider2.Object
            };

            var loader = new DefaultPageLoader(
                providers,
                compilerProvider,
                razorPagesOptions,
                mvcOptions);

            // Act
            var result = loader.Load(new PageActionDescriptor());

            // Assert
            provider1.Verify();
            provider2.Verify();
        }

        [Fact]
        public void Load_InvokesApplicationModelProviders_WithTheRightOrder()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();
            var compilerProvider = GetCompilerProvider();
            var razorPagesOptions = Options.Create(new RazorPagesOptions());
            var mvcOptions = Options.Create(new MvcOptions());

            var provider1 = new Mock<IPageApplicationModelProvider>();
            provider1.SetupGet(p => p.Order).Returns(10);
            var provider2 = new Mock<IPageApplicationModelProvider>();
            provider2.SetupGet(p => p.Order).Returns(-5);

            var sequence = 0;
            provider1.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(1, sequence++);
                    c.PageApplicationModel = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), new object[0]);
                })
                .Verifiable();

            provider2.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(0, sequence++);
                })
                .Verifiable();

            provider1.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(2, sequence++);
                })
                .Verifiable();

            provider2.Setup(p => p.OnProvidersExecuted(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    Assert.Equal(3, sequence++);
                })
                .Verifiable();

            var providers = new[]
            {
                provider1.Object, provider2.Object
            };

            var loader = new DefaultPageLoader(
                providers,
                compilerProvider,
                razorPagesOptions,
                mvcOptions);

            // Act
            var result = loader.Load(new PageActionDescriptor());

            // Assert
            provider1.Verify();
            provider2.Verify();
        }

        [Fact]
        public void Load_InvokesApplicationModelConventions()
        {
            // Arrange
            var descriptor = new PageActionDescriptor();

            var compilerProvider = GetCompilerProvider();
            
            var model = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), new object[0]);
            var provider = new Mock<IPageApplicationModelProvider>();
            provider.Setup(p => p.OnProvidersExecuting(It.IsAny<PageApplicationModelProviderContext>()))
                .Callback((PageApplicationModelProviderContext c) =>
                {
                    c.PageApplicationModel = model;
                });
            var providers = new[] { provider.Object };

            var razorPagesOptions = Options.Create(new RazorPagesOptions());
            var mvcOptions = Options.Create(new MvcOptions());
            var convention = new Mock<IPageApplicationModelConvention>();
            convention.Setup(c => c.Apply(It.IsAny<PageApplicationModel>()))
                .Callback((PageApplicationModel m) =>
                {
                    Assert.Same(model, m);
                });
            razorPagesOptions.Value.Conventions.Add(convention.Object);

            var loader = new DefaultPageLoader(
                providers,
                compilerProvider,
                razorPagesOptions,
                mvcOptions);

            // Act
            var result = loader.Load(new PageActionDescriptor());

            // Assert
            convention.Verify();
        }

        private static IViewCompilerProvider GetCompilerProvider()
        {
            var descriptor = new CompiledViewDescriptor
            {
                ViewAttribute = new RazorPageAttribute("/Views/Index.cshtml", typeof(object), null),
            };

            var compiler = new Mock<IViewCompiler>();
            compiler.Setup(c => c.CompileAsync(It.IsAny<string>()))
                .ReturnsAsync(descriptor);
            var compilerProvider = new Mock<IViewCompilerProvider>();
            compilerProvider.Setup(p => p.GetCompiler())
                .Returns(compiler.Object);
            return compilerProvider.Object;
        }
    }
}
