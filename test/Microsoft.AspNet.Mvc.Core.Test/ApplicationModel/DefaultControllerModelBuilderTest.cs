// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class DefaultControllerModelBuilderTest
    {
        [Fact]
        public void BuildControllerModel_DerivedFromControllerClass_HasFilter()
        {
            // Arrange
            var builder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null),
                                                            NullLoggerFactory.Instance,
                                                            null);
            var typeInfo = typeof(StoreController).GetTypeInfo();

            // Act
            var model = builder.BuildControllerModel(typeInfo);

            // Assert
            var filter = Assert.Single(model.Filters);
            Assert.IsType<ControllerActionFilter>(filter);
        }

        [Fact]
        public void BuildControllerModel_AuthorizeAttributeAddsAuthorizeFilter()
        {
            // Arrange
            var builder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null),
                                                            NullLoggerFactory.Instance,
                                                            null);
            var typeInfo = typeof(AccountController).GetTypeInfo();

            // Act
            var model = builder.BuildControllerModel(typeInfo);

            // Assert
            Assert.True(model.Filters.Any(f => f is AuthorizeFilter));
        }

        // This class has a filter attribute, but doesn't implement any filter interfaces,
        // so ControllerFilter is not present.
        [Fact]
        public void BuildControllerModel_ClassWithoutFilterInterfaces_HasNoControllerFilter()
        {
            // Arrange
            var builder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null),
                                                            NullLoggerFactory.Instance,
                                                            null);
            var typeInfo = typeof(NoFiltersController).GetTypeInfo();

            // Act
            var model = builder.BuildControllerModel(typeInfo);

            // Assert
            var filter = Assert.Single(model.Filters);
            Assert.IsType<ProducesAttribute>(filter);
        }

        [Fact]
        public void BuildControllerModel_ClassWithFilterInterfaces_HasFilter()
        {
            // Arrange
            var builder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null),
                                                            NullLoggerFactory.Instance,
                                                            null);
            var typeInfo = typeof(SomeFiltersController).GetTypeInfo();

            // Act
            var model = builder.BuildControllerModel(typeInfo);

            // Assert
            Assert.Single(model.Filters, f => f is ControllerActionFilter);
            Assert.Single(model.Filters, f => f is ControllerResultFilter);
        }

        [Fact]
        public void BuildControllerModel_ClassWithFilterInterfaces_UnsupportedType()
        {
            // Arrange
            var builder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null),
                                                            NullLoggerFactory.Instance,
                                                            null);
            var typeInfo = typeof(UnsupportedFiltersController).GetTypeInfo();

            // Act
            var model = builder.BuildControllerModel(typeInfo);

            // Assert
            Assert.Empty(model.Filters);
        }

        private class StoreController : Mvc.Controller
        {
        }

        [Produces("application/json")]
        public class NoFiltersController
        {
        }

        [Authorize]
        public class AccountController
        {
        }

        public class SomeFiltersController : IAsyncActionFilter, IResultFilter
        {
            public Task OnActionExecutionAsync(
                [NotNull] ActionExecutingContext context,
                [NotNull] ActionExecutionDelegate next)
            {
                return null;
            }

            public void OnResultExecuted([NotNull] ResultExecutedContext context)
            {
            }

            public void OnResultExecuting([NotNull]ResultExecutingContext context)
            {
            }
        }

        private class UnsupportedFiltersController : IExceptionFilter, IAuthorizationFilter, IAsyncResourceFilter
        {
            public void OnAuthorization([NotNull]AuthorizationContext context)
            {
                throw new NotImplementedException();
            }

            public void OnException([NotNull]ExceptionContext context)
            {
                throw new NotImplementedException();
            }

            public Task OnResourceExecutionAsync([NotNull]ResourceExecutingContext context, [NotNull]ResourceExecutionDelegate next)
            {
                throw new NotImplementedException();
            }
        }
    }
}