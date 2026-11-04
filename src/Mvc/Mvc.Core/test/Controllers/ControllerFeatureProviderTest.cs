// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ControllerFeatureProviderControllers;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    public class ControllerFeatureProviderTest
    {
        [Fact]
        public void UserDefinedClass_DerivedFromController_IsController()
        {
            // Arrange
            var controllerType = typeof(StoreController).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(controllerType, discovered);
        }

        [Fact]
        public void UserDefinedClass_DerivedFromControllerBase_IsController()
        {
            // Arrange
            var controllerType = typeof(ProductsController).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(controllerType, discovered);
        }

        [Fact]
        public void UserDefinedClass_DerivedFromControllerBaseWithoutSuffix_IsController()
        {
            // Arrange
            var controllerType = typeof(Products).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(controllerType, discovered);
        }

        [Fact]
        public void FrameworkControllerClass_IsNotController()
        {
            // Arrange
            var controllerType = typeof(Controller).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void FrameworkBaseControllerClass_IsNotController()
        {
            // Arrange
            var controllerType = typeof(ControllerBase).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void UserDefinedControllerClass_IsNotController()
        {
            // Arrange
            var controllerType = typeof(ControllerFeatureProviderControllers.Controller).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void Interface_IsNotController()
        {
            // Arrange
            var controllerType = typeof(ITestController).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void ValueTypeClass_IsNotController()
        {
            // Arrange
            var controllerType = typeof(int).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void AbstractClass_IsNotController()
        {
            // Arrange
            var controllerType = typeof(AbstractController).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void DerivedAbstractClass_IsController()
        {
            // Arrange
            var controllerType = typeof(DerivedAbstractController).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(controllerType, discovered);
        }

        [Fact]
        public void OpenGenericClass_IsNotController()
        {
            // Arrange
            var controllerType = typeof(OpenGenericController<>).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void WithoutSuffixOrAncestorWithController_IsNotController()
        {
            // Arrange
            var controllerType = typeof(NoSuffixPoco).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void ClosedGenericClass_IsController()
        {
            // Arrange
            var controllerType = typeof(OpenGenericController<string>).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(controllerType, discovered);
        }

        [Fact]
        public void DerivedGenericClass_IsController()
        {
            // Arrange
            var controllerType = typeof(DerivedGenericController).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(controllerType, discovered);
        }

        [Fact]
        public void Poco_WithNamingConvention_IsController()
        {
            // Arrange
            var controllerType = typeof(PocoController).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(controllerType, discovered);
        }

        [Fact]
        public void NoControllerSuffix_IsController()
        {
            // Arrange
            var controllerType = typeof(NoSuffix).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(controllerType, discovered);
        }

        [Theory]
        [InlineData(typeof(DescendantLevel1))]
        [InlineData(typeof(DescendantLevel2))]
        public void AncestorTypeHasControllerAttribute_IsController(Type type)
        {
            // Arrange
            var manager = GetApplicationPartManager(type.GetTypeInfo());
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var discovered = Assert.Single(feature.Controllers);
            Assert.Equal(type.GetTypeInfo(), discovered);
        }

        [Fact]
        public void AncestorTypeDoesNotHaveControllerAttribute_IsNotController()
        {
            // Arrange
            var controllerType = typeof(NoSuffixNoControllerAttribute).GetTypeInfo();
            var manager = GetApplicationPartManager(controllerType);
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        [Fact]
        public void GetFeature_OnlyRunsOnParts_ThatImplementIApplicationPartTypeProvider()
        {
            // Arrange
            var otherPart = new Mock<ApplicationPart>();
            otherPart
                .As<IApplicationPartTypeProvider>()
                .Setup(t => t.Types)
                .Returns(new[] { typeof(PocoController).GetTypeInfo() });

            var parts = new[] {
                Mock.Of<ApplicationPart>(),
                new TestApplicationPart(typeof(NoSuffix).GetTypeInfo()),
                otherPart.Object
            };

            var feature = new ControllerFeature();

            var expected = new List<TypeInfo>
            {
                typeof(NoSuffix).GetTypeInfo(),
                typeof(PocoController).GetTypeInfo()
            };

            var provider = new ControllerFeatureProvider();

            // Act
            provider.PopulateFeature(parts, feature);

            // Assert
            Assert.Equal(expected, feature.Controllers.ToList());
        }

        [Fact]
        public void GetFeature_DoesNotAddDuplicates_ToTheListOfControllers()
        {
            // Arrange
            var otherPart = new Mock<ApplicationPart>();
            otherPart
                .As<IApplicationPartTypeProvider>()
                .Setup(t => t.Types)
                .Returns(new[] { typeof(PocoController).GetTypeInfo() });

            var parts = new[] {
                Mock.Of<ApplicationPart>(),
                new TestApplicationPart(typeof(NoSuffix)),
                otherPart.Object
            };

            var feature = new ControllerFeature();

            var expected = new List<TypeInfo>
            {
                typeof(NoSuffix).GetTypeInfo(),
                typeof(PocoController).GetTypeInfo()
            };

            var provider = new ControllerFeatureProvider();

            provider.PopulateFeature(parts, feature);

            // Act
            provider.PopulateFeature(parts, feature);

            // Assert
            Assert.Equal(expected, feature.Controllers.ToList());
        }

        [Theory]
        [InlineData(typeof(BaseNonControllerController))]
        [InlineData(typeof(BaseNonControllerControllerChild))]
        [InlineData(typeof(BasePocoNonControllerController))]
        [InlineData(typeof(BasePocoNonControllerControllerChild))]
        [InlineData(typeof(NonController))]
        [InlineData(typeof(NonControllerChild))]
        [InlineData(typeof(BaseNonControllerAttributeChildControllerControllerAttributeController))]
        [InlineData(typeof(PersonModel))] // Verifies that POCO type hierarchies that don't derive from controller return false.
        public void IsController_ReturnsFalse_IfTypeOrAncestorHasNonControllerAttribute(Type type)
        {
            // Arrange
            var manager = GetApplicationPartManager(type.GetTypeInfo());
            var feature = new ControllerFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.Controllers);
        }

        private static ApplicationPartManager GetApplicationPartManager(params TypeInfo[] types)
        {
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(types));
            manager.FeatureProviders.Add(new ControllerFeatureProvider());

            return manager;
        }
    }
}

// These controllers are used to test the ControllerFeatureProvider implementation
// which REQUIRES that they be public top-level classes. To avoid having to stub out the
// implementation of this class to test it, they are just top level classes. Don't reuse
// these outside this test - find a better way or use nested classes to keep the tests
// independent.
namespace Microsoft.AspNetCore.Mvc.ControllerFeatureProviderControllers
{
    public abstract class AbstractController : Controller
    {
    }

    public class DerivedAbstractController : AbstractController
    {
    }

    public class StoreController : Controller
    {
    }

    public class ProductsController : ControllerBase
    {
    }

    public class Products : ControllerBase
    {
    }

    [Controller]
    public abstract class Controller
    {
    }

    public abstract class NoControllerAttributeBaseController
    {
    }

    public class NoSuffixNoControllerAttribute : NoControllerAttributeBaseController
    {
    }

    public class OpenGenericController<T> : Controller
    {
    }

    public class DerivedGenericController : OpenGenericController<string>
    {
    }

    public interface ITestController
    {
    }

    public class NoSuffix : Controller
    {
    }

    public class NoSuffixPoco
    {

    }

    public class PocoController
    {
    }

    [Controller]
    public class CustomBase
    {

    }

    [Controller]
    public abstract class CustomAbstractBaseController
    {

    }

    public class DescendantLevel1 : CustomBase
    {

    }

    public class DescendantLevel2 : DescendantLevel1
    {

    }

    public class AbstractChildWithoutSuffix : CustomAbstractBaseController
    {

    }

    [NonController]
    public class BasePocoNonControllerController
    {

    }

    [Controller]
    public class BaseNonControllerAttributeChildControllerControllerAttributeController : BaseNonControllerController
    {

    }

    public class BasePocoNonControllerControllerChild : BasePocoNonControllerController
    {

    }

    [NonController]
    public class BaseNonControllerController : Controller
    {

    }

    public class BaseNonControllerControllerChild : BaseNonControllerController
    {

    }

    [NonController]
    public class NonControllerChild : Controller
    {

    }

    [NonController]
    public class NonController : Controller
    {

    }

    public class DataModelBase
    {

    }

    public class EntityDataModel : DataModelBase
    {

    }

    public class PersonModel : EntityDataModel
    {

    }
}
