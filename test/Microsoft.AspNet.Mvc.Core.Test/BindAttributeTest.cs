// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Internal;
#if DNX451
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class BindAttributeTest
    {
        [Fact]
        public void Constructor_Throws_IfTypeDoesNotImplement_IPropertyBindingPredicateProvider()
        {
            // Arrange
            var expected =
                "The type 'Microsoft.AspNet.Mvc.ModelBinding.BindAttributeTest+UnrelatedType' " +
                "does not implement the interface " +
                "'Microsoft.AspNet.Mvc.ModelBinding.IPropertyBindingPredicateProvider'." +
                Environment.NewLine +
                "Parameter name: predicateProviderType";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new BindAttribute(typeof(UnrelatedType)));
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [InlineData(typeof(DerivedProvider))]
        [InlineData(typeof(BaseProvider))]
        public void Constructor_SetsThe_PropertyFilterProviderType_ForValidTypes(Type type)
        {
            // Arrange
            var attribute = new BindAttribute(type);

            // Act & Assert
            Assert.Equal(type, attribute.PredicateProviderType);
        }

        [Theory]
        [InlineData("UserName", true)]
        [InlineData("Username", false)]
        [InlineData("Password", false)]
        [InlineData("LastName", true)]
        [InlineData("MiddleName", true)]
        [InlineData(" ", false)]
        [InlineData("foo", true)]
        [InlineData("bar", true)]
        public void BindAttribute_Include(string property, bool isIncluded)
        {
            // Arrange
            var bind = new BindAttribute(new string[] { "UserName", "FirstName", "LastName, MiddleName,  ,foo,bar " });

            var context = new ModelBindingContext();

            // Act
            var predicate = bind.PropertyFilter;

            // Assert
            Assert.Equal(isIncluded, predicate(context, property));
        }

#if DNX451
        [Theory]
        [InlineData("UserName", true)]
        [InlineData("Username", false)]
        [InlineData("Password", false)]
        public void BindAttribute_ProviderType(string property, bool isIncluded)
        {
            // Arrange
            var bind = new BindAttribute(typeof(TestProvider));

            var context = new ModelBindingContext();
            context.OperationBindingContext = new OperationBindingContext()
            {
                HttpContext = new DefaultHttpContext(),
            };
            var services = new Mock<IServiceProvider>();

            context.OperationBindingContext.HttpContext.RequestServices = services.Object;

            // Act
            var predicate = bind.PropertyFilter;

            // Assert
            Assert.Equal(isIncluded, predicate(context, property));
        }

        // Each time .PropertyFilter is called, a since instance of the provider should
        // be created and cached.
        [Fact]
        public void BindAttribute_ProviderType_Cached()
        {
            // Arrange
            var bind = new BindAttribute(typeof(TestProvider));

            var context = new ModelBindingContext();
            context.OperationBindingContext = new OperationBindingContext()
            {
                HttpContext = new DefaultHttpContext(),
            };

            var services = new Mock<IServiceProvider>(MockBehavior.Strict);

            context.OperationBindingContext.HttpContext.RequestServices = services.Object;

            // Act
            var predicate = bind.PropertyFilter;

            // Assert
            Assert.True(predicate(context, "UserName"));
            Assert.True(predicate(context, "UserName"));
        }
#endif

        private class TestProvider : IPropertyBindingPredicateProvider
        {
            public Func<ModelBindingContext, string, bool> PropertyFilter
            {
                get
                {
                    return (context, property) => string.Equals(property, "UserName", StringComparison.Ordinal);
                }
            }
        }

        private class BaseProvider : IPropertyBindingPredicateProvider
        {
            public Func<ModelBindingContext, string, bool> PropertyFilter
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }

        private class DerivedProvider : BaseProvider
        {
        }

        private class UnrelatedType
        {
        }
    }
}
