// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class FilterCollectionExtensionsTest
    {
        [Fact]
        public void Add_UsesTypeFilterAttribute()
        {
            // Arrange
            var collection = new Collection<IFilter>();

            // Act
            var added = collection.Add(typeof(MyFilter));

            // Assert
            var typeFilter = Assert.IsType<TypeFilterAttribute>(added);
            Assert.Equal(typeof(MyFilter), typeFilter.ImplementationType);
            Assert.Same(typeFilter, Assert.Single(collection));
        }

        [Fact]
        public void Add_WithOrder_SetsOrder()
        {
            // Arrange
            var collection = new Collection<IFilter>();

            // Act
            var added = collection.Add(typeof(MyFilter), 17);

            // Assert
            Assert.Equal(17, Assert.IsAssignableFrom<IOrderedFilter>(added).Order);
        }

        [Fact]
        public void Add_ThrowsOnNonIFilter()
        {
            // Arrange
            var collection = new Collection<IFilter>();

            var expectedMessage =
                "The type 'Microsoft.AspNet.Mvc.FilterCollectionExtensionsTest+NonFilter' must derive from " +
                "'Microsoft.AspNet.Mvc.IFilter'." + Environment.NewLine +
                "Parameter name: filterType";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => { collection.Add(typeof(NonFilter)); });

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void AddService_UsesServiceFilterAttribute()
        {
            // Arrange
            var collection = new Collection<IFilter>();

            // Act
            var added = collection.AddService(typeof(MyFilter));

            // Assert
            var serviceFilter = Assert.IsType<ServiceFilterAttribute>(added);
            Assert.Equal(typeof(MyFilter), serviceFilter.ServiceType);
            Assert.Same(serviceFilter, Assert.Single(collection));
        }

        [Fact]
        public void AddService_SetsOrder()
        {
            // Arrange
            var collection = new Collection<IFilter>();

            // Act
            var added = collection.AddService(typeof(MyFilter), 17);

            // Assert
            Assert.Equal(17, Assert.IsAssignableFrom<IOrderedFilter>(added).Order);
        }

        [Fact]
        public void AddService_ThrowsOnNonIFilter()
        {
            // Arrange
            var collection = new Collection<IFilter>();

            var expectedMessage =
                "The type 'Microsoft.AspNet.Mvc.FilterCollectionExtensionsTest+NonFilter' must derive from " +
                "'Microsoft.AspNet.Mvc.IFilter'." + Environment.NewLine +
                "Parameter name: filterType";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => { collection.AddService(typeof(NonFilter)); });

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        private class MyFilter : IFilter, IOrderedFilter
        {
            public int Order
            {
                get;
                set;
            }
        }

        private class NonFilter
        {
        }
    }
}