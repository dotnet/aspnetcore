// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    public class DefaultViewComponentFactoryTest
    {
        [Fact]
        public void CreateViewComponent_ActivatesProperties_OnTheInstance()
        {
            // Arrange
            var context = new ViewComponentContext
            {
            };

            var component = new ActivablePropertiesViewComponent();
            var activator = new Mock<IViewComponentActivator>();
            activator.Setup(a => a.Create(context))
                .Returns(component);

            var factory = new DefaultViewComponentFactory(activator.Object);

            // Act
            var result = factory.CreateViewComponent(context);

            // Assert
            var activablePropertiesComponent = Assert.IsType<ActivablePropertiesViewComponent>(result);

            Assert.Same(component, activablePropertiesComponent);
            Assert.Same(component.Context, activablePropertiesComponent.Context);
        }

        [Fact]
        public void ReleaseViewComponent_CallsDispose_OnTheInstance()
        {
            // Arrange
            var context = new ViewComponentContext
            {
            };

            var component = new ActivablePropertiesViewComponent();

            var viewComponentActivator = new Mock<IViewComponentActivator>();
            viewComponentActivator.Setup(vca => vca.Release(context, component))
                .Callback<ViewComponentContext, object>((c, o) => (o as IDisposable)?.Dispose());

            var factory = new DefaultViewComponentFactory(viewComponentActivator.Object);

            // Act
            factory.ReleaseViewComponent(context, component);

            // Assert
            Assert.True(component.Disposed);
        }
    }

    public class ActivablePropertiesViewComponent : IDisposable
    {
        [ViewComponentContext]
        public ViewComponentContext Context { get; set; }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }

        public string Invoke()
        {
            return "something";
        }
    }
}
