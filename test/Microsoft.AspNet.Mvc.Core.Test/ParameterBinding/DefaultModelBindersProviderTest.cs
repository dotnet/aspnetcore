// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class DefaultModelBindersProviderTest
    {
        [Fact]
        public void DefaultModelBindersProvider_ProvidesInstancesOfModelBinders()
        {
            // Arrange
            var binder = new TypeMatchModelBinder();
            var options = new MvcOptions();
            options.ModelBinders.Clear();
            options.ModelBinders.Add(binder);
            options.ModelBinders.Add(typeof(GenericModelBinder));
            var optionsAccessor = new Mock<IOptionsAccessor<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options)
                           .Returns(options);
            var activator = new Mock<ITypeActivator>();
            var serviceProvider = Mock.Of<IServiceProvider>();
            activator.Setup(a => a.CreateInstance(serviceProvider, typeof(GenericModelBinder)))
                     .Returns(new GenericModelBinder(serviceProvider, activator.Object))
                     .Verifiable();

            var provider = new DefaultModelBindersProvider(optionsAccessor.Object, activator.Object, serviceProvider);

            // Act
            var binders = provider.ModelBinders;

            // Assert
            Assert.Equal(2, binders.Count);
            Assert.Same(binder, binders[0]);
            Assert.IsType<GenericModelBinder>(binders[1]);
        }
    }
}
#endif