// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.ApplicationModels.DefaultControllerModelBuilderTestControllers;
using Xunit;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class DefaultControllerModelBuilderTest
    {
        [Fact]
        public void IsController_UserDefinedClass()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(StoreController).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_FrameworkControllerClass()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(Controller).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_UserDefinedControllerClass()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(DefaultControllerModelBuilderTestControllers.Controller).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_Interface()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(IController).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_AbstractClass()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(AbstractController).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_DerivedAbstractClass()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(DerivedAbstractController).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_OpenGenericClass()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(OpenGenericController<>).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.False(isController);
        }

        [Fact]
        public void IsController_ClosedGenericClass()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(OpenGenericController<string>).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_DerivedGenericClass()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(DerivedGenericController).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_Poco_WithNamingConvention()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(PocoController).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void IsController_NoControllerSuffix()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(NoSuffix).GetTypeInfo();

            // Act
            var isController = builder.IsController(typeInfo);

            // Assert
            Assert.True(isController);
        }

        [Fact]
        public void BuildControllerModel_DerivedFromControllerClass_HasFilter()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(StoreController).GetTypeInfo();

            // Act
            var model = builder.BuildControllerModel(typeInfo);

            // Assert
            var filter = Assert.Single(model.Filters);
            Assert.IsType<ControllerActionFilter>(filter);
        }

        // This class has a filter attribute, but doesn't implement any filter interfaces,
        // so ControllerFilter is not present.
        [Fact]
        public void BuildControllerModel_ClassWithoutFilterInterfaces_HasNoControllerFilter()
        {
            // Arrange
            var builder = new AccessibleControllerModelBuilder();
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
            var builder = new AccessibleControllerModelBuilder();
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
            var builder = new AccessibleControllerModelBuilder();
            var typeInfo = typeof(UnsupportedFiltersController).GetTypeInfo();

            // Act
            var model = builder.BuildControllerModel(typeInfo);

            // Assert
            Assert.Empty(model.Filters);
        }

        private class AccessibleControllerModelBuilder : DefaultControllerModelBuilder
        {
            public AccessibleControllerModelBuilder()
                : base(new DefaultActionModelBuilder(), new NullLoggerFactory())
            {
            }

            public new bool IsController([NotNull]TypeInfo typeInfo)
            {
                return base.IsController(typeInfo);
            }
        }
    }
}

// These controllers are used to test the DefaultActionDiscoveryConventions implementation
// which REQUIRES that they be public top-level classes. To avoid having to stub out the
// implementation of this class to test it, they are just top level classes. Don't reuse
// these outside this test - find a better way or use nested classes to keep the tests
// independent.
namespace Microsoft.AspNet.Mvc.ApplicationModels.DefaultControllerModelBuilderTestControllers
{
    public abstract class AbstractController : Mvc.Controller
    {
    }

    public class DerivedAbstractController : AbstractController
    {
    }

    public class StoreController : Mvc.Controller
    {
    }

    public class Controller
    {
    }

    public class OpenGenericController<T> : Mvc.Controller
    {
    }

    public class DerivedGenericController : OpenGenericController<string>
    {
    }

    public interface IController
    {
    }

    public class NoSuffix : Mvc.Controller
    {
    }

    public class PocoController
    {
    }

    [Produces("application/json")]
    public class NoFiltersController
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

        public void OnResultExecuting([NotNull ]ResultExecutingContext context)
        {
        }
    }

    public class UnsupportedFiltersController : IExceptionFilter, IAuthorizationFilter, IAsyncResourceFilter
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