// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    public class DefaultValidationExcludeFiltersProviderTests
    {
        [Theory]
        [InlineData(typeof(BaseType))]
        [InlineData(typeof(DerivedType))]
        public void Add_WithType_RegistersTypesAndDerivedType_ToBeExcluded(Type type)
        {
            // Arrange
            var options = new MvcOptions();
            options.ValidationExcludeFilters.Add(type);
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options)
                           .Returns(options);
            var typeActivatorCache = new Mock<ITypeActivatorCache>();
            var service = Mock.Of<ITestService>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);
            var provider = new DefaultValidationExcludeFiltersProvider(optionsAccessor.Object,
                                                                       typeActivatorCache.Object,
                                                                       serviceProvider.Object);

            // Act
            var filters = provider.ExcludeFilters;

            // Assert
            Assert.Equal(1, filters.Count);
            Assert.True(filters[0].IsTypeExcluded(type));
        }

        [Theory]
        [InlineData(typeof(BaseType))]
        [InlineData(typeof(UnRelatedType))]
        public void Add_RegisterDerivedType_BaseAndUnrealatedTypesAreNotExcluded(Type type)
        {
            // Arrange
            var options = new MvcOptions();
            options.ValidationExcludeFilters.Add(typeof(DerivedType));
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options)
                           .Returns(options);
            var typeActivatorCache = new Mock<ITypeActivatorCache>();
            var service = Mock.Of<ITestService>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);
            var provider = new DefaultValidationExcludeFiltersProvider(optionsAccessor.Object,
                                                                       typeActivatorCache.Object,
                                                                       serviceProvider.Object);

            // Act
            var filters = provider.ExcludeFilters;

            // Assert
            Assert.Equal(1, filters.Count);
            Assert.False(filters[0].IsTypeExcluded(type));
        }

        [Theory]
        [InlineData(typeof(BaseType))]
        [InlineData(typeof(DerivedType))]
        public void Add_WithTypeName_RegistersTypesAndDerivedType_ToBeExcluded(Type type)
        {
            // Arrange
            var options = new MvcOptions();
            options.ValidationExcludeFilters.Add(type.FullName);
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options)
                           .Returns(options);
            var typeActivatorCache = new Mock<ITypeActivatorCache>();
            var service = Mock.Of<ITestService>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);
            var provider = new DefaultValidationExcludeFiltersProvider(optionsAccessor.Object,
                                                                       typeActivatorCache.Object,
                                                                       serviceProvider.Object);

            // Act
            var filters = provider.ExcludeFilters;

            // Assert
            Assert.Equal(1, filters.Count);
            Assert.True(filters[0].IsTypeExcluded(type));
        }

        [Theory]
        [InlineData(typeof(BaseType))]
        [InlineData(typeof(UnRelatedType))]
        [InlineData(typeof(SubNameSpace.UnRelatedType))]
        public void Add_WithTypeName_RegisterDerivedType_BaseAndUnrealatedTypesAreNotExcluded(Type type)
        {
            // Arrange
            var options = new MvcOptions();
            options.ValidationExcludeFilters.Add(typeof(DerivedType).FullName);
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options)
                           .Returns(options);
            var service = Mock.Of<ITestService>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(ITestService)))
                           .Returns(service);
            var typeActivatorCache = new Mock<ITypeActivatorCache>();
            var provider = new DefaultValidationExcludeFiltersProvider(optionsAccessor.Object,
                                                                       typeActivatorCache.Object,
                                                                       serviceProvider.Object);

            // Act
            var filters = provider.ExcludeFilters;

            // Assert
            Assert.Equal(1, filters.Count);
            Assert.False(filters[0].IsTypeExcluded(type));
        }

        private class BaseType
        {
        }

        private class DerivedType : BaseType
        {
        }

        private class UnRelatedType
        {
        }
    }

    namespace SubNameSpace
    {
        public class UnRelatedType
        {
        }
    }

    public interface ITestService
    {
    }
}


