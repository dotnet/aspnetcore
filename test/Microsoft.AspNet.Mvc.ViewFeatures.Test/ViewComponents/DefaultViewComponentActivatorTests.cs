// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentActivatorTests
    {
        [Fact]
        public void DefaultViewComponentActivator_ActivatesViewComponentContext()
        {
            // Arrange
            var activator = new DefaultViewComponentActivator();

            var context = new ViewComponentContext();
            var instance = new TestViewComponent();

            // Act
            activator.Activate(instance, context);

            // Assert
            Assert.Same(context, instance.ViewComponentContext);
        }

        [Fact]
        public void DefaultViewComponentActivator_ActivatesViewComponentContext_IgnoresNonPublic()
        {
            // Arrange
            var activator = new DefaultViewComponentActivator();

            var context = new ViewComponentContext();
            var instance = new VisibilityViewComponent();

            // Act
            activator.Activate(instance, context);

            // Assert
            Assert.Same(context, instance.ViewComponentContext);
            Assert.Null(instance.C);
        }

        private class TestViewComponent : ViewComponent
        {
            public Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class VisibilityViewComponent : ViewComponent
        {
            [ViewComponentContext]
            protected internal ViewComponentContext C { get; set; }
        }
    }
}
