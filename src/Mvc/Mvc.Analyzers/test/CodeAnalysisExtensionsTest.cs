// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

public class CodeAnalysisExtensionsTest
{
    [Fact]
    public void GetAttributes_OnMethodWithoutAttributes()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class TestController
    {
        public void Method() { }
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestController");
        var method = (IMethodSymbol)testClass.GetMembers("Method").First();

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

        // Assert
        Assert.Empty(attributes);
    }

    [Fact]
    public void GetAttributes_OnNonOverriddenMethod_ReturnsAllAttributesOnCurrentAction()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class TestClass
    {
        [ProducesResponseType(201)]
        public void Method() { }
    }
}
";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");
        var method = (IMethodSymbol)testClass.GetMembers("Method").First();

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

        // Assert
        Assert.Collection(
            attributes,
            attributeData => Assert.Equal(201, attributeData.ConstructorArguments[0].Value));
    }

    [Fact]
    public void GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentAction()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class BaseClass
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void Method() { }
    }

    public class TestClass : BaseClass
    {
        [ProducesResponseType(400)]
        public override void Method() { }
    }
}
";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");
        var method = (IMethodSymbol)testClass.GetMembers("Method").First();

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: false);

        // Assert
        Assert.Collection(
            attributes,
            attributeData => Assert.Equal(400, attributeData.ConstructorArguments[0].Value));
    }

    [Fact]
    public void GetAttributesSymbolOverload_OnMethodSymbol()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class BaseClass
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void Method() { }
    }

    public class TestClass : BaseClass
    {
        [ProducesResponseType(400)]
        public override void Method() { }
    }
}
";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");
        var method = (IMethodSymbol)testClass.GetMembers("Method").First();

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute);

        // Assert
        Assert.Collection(
            attributes,
            attributeData => Assert.Equal(400, attributeData.ConstructorArguments[0].Value));
    }

    [Fact]
    public void GetAttributes_WithInheritTrue_ReturnsAllAttributesOnCurrentActionAndOverridingMethod()
    {
        /// Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class BaseClass
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void Method() { }
    }

    public class TestClass : BaseClass
    {
        [ProducesResponseType(400)]
        public override void Method() { }
    }
}
";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");
        var method = (IMethodSymbol)testClass.GetMembers("Method").First();

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

        // Assert
        Assert.Collection(
            attributes,
            attributeData => Assert.Equal(400, attributeData.ConstructorArguments[0].Value),
            attributeData => Assert.Equal(200, attributeData.ConstructorArguments[0].Value),
            attributeData => Assert.Equal(404, attributeData.ConstructorArguments[0].Value));
    }

    [Fact]
    public void GetAttributes_OnNewMethodOfVirtualBaseMethod()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class BaseClass
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void VirtualMethod() { }

        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void NotVirtualMethod() { }
    }

    public class TestClass : BaseClass
    {
        [ProducesResponseType(400)]
        public new void VirtualMethod() { }

        [ProducesResponseType(401)]
        public new void NotVirtualMethod() { }
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");
        var method = (IMethodSymbol)testClass.GetMembers("VirtualMethod").First();

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

        // Assert
        Assert.Collection(
            attributes,
            attributeData => Assert.Equal(400, attributeData.ConstructorArguments[0].Value));
    }

    [Fact]
    public void GetAttributes_OnNewMethodOfNonVirtualBaseMethod()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class BaseClass
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void VirtualMethod() { }

        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public virtual void NotVirtualMethod() { }
    }

    public class TestClass : BaseClass
    {
        [ProducesResponseType(400)]
        public new void VirtualMethod() { }

        [ProducesResponseType(401)]
        public new void NotVirtualMethod() { }
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");
        var method = (IMethodSymbol)testClass.GetMembers("NotVirtualMethod").First();

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

        // Assert
        Assert.Collection(
            attributes,
            attributeData => Assert.Equal(401, attributeData.ConstructorArguments[0].Value));
    }

    [Fact]
    public void GetAttributes_OnTypeWithoutAttributes()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class TestClass
    {
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(testClass, attribute, inherit: true);

        // Assert
        Assert.Empty(attributes);
    }

    [Fact]
    public void GetAttributes_OnTypeWithAttributes()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    [ApiConventionType(typeof(object))]
    [ApiController]
    [ApiConventionType(typeof(string))]
    public class TestClass
    {
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(testClass, attribute, inherit: true);

        // Assert
        Assert.Collection(
            attributes,
            attributeData =>
            {
                Assert.Same(attribute, attributeData.AttributeClass);
                Assert.Equal(attributeData.ConstructorArguments[0].Value, compilation.GetSpecialType(SpecialType.System_Object));
            },
            attributeData =>
            {
                Assert.Same(attribute, attributeData.AttributeClass);
                Assert.Equal(attributeData.ConstructorArguments[0].Value, compilation.GetSpecialType(SpecialType.System_String));
            });
    }

    [Fact]
    public void GetAttributes_BaseTypeWithAttributes()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    [ApiConventionType(typeof(object))]
    [ApiController]
    [ApiConventionType(typeof(string))]
    public class BaseType
    {
    }

    [ApiConventionType(typeof(int))]
    public class TestClass : BaseType
    {
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(testClass, attribute, inherit: true);

        // Assert
        Assert.Collection(
            attributes,
            attributeData =>
            {
                Assert.Same(attribute, attributeData.AttributeClass);
                Assert.Equal(attributeData.ConstructorArguments[0].Value, compilation.GetSpecialType(SpecialType.System_Int32));
            },
            attributeData =>
            {
                Assert.Same(attribute, attributeData.AttributeClass);
                Assert.Equal(attributeData.ConstructorArguments[0].Value, compilation.GetSpecialType(SpecialType.System_Object));
            },
            attributeData =>
            {
                Assert.Same(attribute, attributeData.AttributeClass);
                Assert.Equal(attributeData.ConstructorArguments[0].Value, compilation.GetSpecialType(SpecialType.System_String));
            });
    }

    [Fact]
    public void GetAttributes_OnDerivedTypeWithInheritFalse()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    [ApiConventionType(typeof(object))]
    [ApiController]
    [ApiConventionType(typeof(string))]
    public class BaseType
    {
    }

    [ApiConventionType(typeof(int))]
    public class TestClass : BaseType
    {
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(testClass, attribute, inherit: false);

        // Assert
        Assert.Collection(
            attributes,
            attributeData =>
            {
                Assert.Same(attribute, attributeData.AttributeClass);
                Assert.Equal(attributeData.ConstructorArguments[0].Value, compilation.GetSpecialType(SpecialType.System_Int32));
            });
    }

    [Fact]
    public void GetAttributesSymbolOverload_OnTypeSymbol()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    [ApiConventionType(typeof(object))]
    [ApiController]
    [ApiConventionType(typeof(string))]
    public class BaseType
    {
    }

    [ApiConventionType(typeof(int))]
    public class TestClass : BaseType
    {
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestClass");

        // Act
        var attributes = CodeAnalysisExtensions.GetAttributes(symbol: testClass, attribute: attribute);

        // Assert
        Assert.Collection(
            attributes,
            attributeData =>
            {
                Assert.Same(attribute, attributeData.AttributeClass);
                Assert.Equal(attributeData.ConstructorArguments[0].Value, compilation.GetSpecialType(SpecialType.System_Int32));
            });
    }

    [Fact]
    public void HasAttribute_ReturnsFalseIfSymbolDoesNotHaveAttribute()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttribute : Attribute { }

    [Controller]
    public class HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttributeTest
    {
        [NonAction]
        public void SomeMethod() { }

        [BindProperty]
        public string SomeProperty { get; set; }
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName("TestApp.HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttribute");
        var testClass = compilation.GetTypeByMetadataName("TestApp.HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttributeTest");
        var testMethod = (IMethodSymbol)testClass.GetMembers("SomeMethod").First();
        var testProperty = (IPropertySymbol)testClass.GetMembers("SomeProperty").First();

        // Act
        var classHasAttribute = CodeAnalysisExtensions.HasAttribute(testClass, attribute, inherit: false);
        var methodHasAttribute = CodeAnalysisExtensions.HasAttribute(testMethod, attribute, inherit: false);
        var propertyHasAttribute = CodeAnalysisExtensions.HasAttribute(testProperty, attribute, inherit: false);

        // AssertControllerAttribute
        Assert.False(classHasAttribute);
        Assert.False(methodHasAttribute);
        Assert.False(propertyHasAttribute);
    }

    [Fact]
    public void HasAttribute_ReturnsTrueIfTypeHasAttribute()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    [Controller]
    public class TestController { }
}";

        var compilation = TestCompilation.Create(source);

        var attribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerAttribute");
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestController");

        // Act
        var hasAttribute = CodeAnalysisExtensions.HasAttribute(testClass, attribute, inherit: false);

        // Assert
        Assert.True(hasAttribute);
    }

    [Fact]
    public void HasAttribute_ReturnsTrueIfBaseTypeHasAttribute()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    [Controller]
    public class TestControllerBase { }

    public class TestController : TestControllerBase { }
}";

        var compilation = TestCompilation.Create(source);

        var attribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerAttribute");
        var testClass = compilation.GetTypeByMetadataName($"TestApp.TestController");

        // Act
        var hasAttributeWithoutInherit = CodeAnalysisExtensions.HasAttribute(testClass, attribute, inherit: false);
        var hasAttributeWithInherit = CodeAnalysisExtensions.HasAttribute(testClass, attribute, inherit: true);

        // Assert
        Assert.False(hasAttributeWithoutInherit);
        Assert.True(hasAttributeWithInherit);
    }

    [Fact]
    public void HasAttribute_ReturnsTrueForInterfaceContractOnAttribute()
    {
        // Arrange
        var source = @"
using System;
namespace TestApp
{
    public interface ITestInterface { }

    public class TestAttribute : Attribute, ITestInterface  { }

    [TestAttribute]
    public class TestController
    {
    }
}";

        var compilation = TestCompilation.Create(source);
        var @interface = compilation.GetTypeByMetadataName("TestApp.ITestInterface");
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestController");

        // Act
        var hasAttribute = CodeAnalysisExtensions.HasAttribute(testClass, @interface, inherit: true);
        var hasAttributeOnDerived = CodeAnalysisExtensions.HasAttribute(testClass, @interface, inherit: true);

        // Assert
        Assert.True(hasAttribute);
        Assert.True(hasAttributeOnDerived);
    }

    [Fact]
    public void HasAttribute_ReturnsTrueForAttributesOnMethods()
    {
        // Arrange
        var source = @"
using System;
namespace TestApp
{
    public class TestAttribute : Attribute { }

    public class TestController
    {
        [TestAttribute]
        public void SomeMethod() { }
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName("TestApp.TestAttribute");
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestController");
        var method = (IMethodSymbol)testClass.GetMembers("SomeMethod").First();

        // Act
        var hasAttribute = CodeAnalysisExtensions.HasAttribute(method, attribute, inherit: false);

        // Assert
        Assert.True(hasAttribute);
    }

    [Fact]
    public void HasAttribute_ReturnsTrueForAttributesOnOverriddenMethods()
    {
        // Arrange
        var source = @"
using System;
namespace TestApp
{
    public class TestAttribute : Attribute { }

    public class TestControllerBase
    {
        [TestAttribute]
        public virtual void SomeMethod() { }
    }

    public class TestController : TestControllerBase
    {
        public override void SomeMethod() { }
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName("TestApp.TestAttribute");
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestController");
        var method = (IMethodSymbol)testClass.GetMembers("SomeMethod").First();

        // Act
        var hasAttributeWithoutInherit = CodeAnalysisExtensions.HasAttribute(method, attribute, inherit: false);
        var hasAttributeWithInherit = CodeAnalysisExtensions.HasAttribute(method, attribute, inherit: true);

        // Assert
        Assert.False(hasAttributeWithoutInherit);
        Assert.True(hasAttributeWithInherit);
    }

    [Fact]
    public void HasAttribute_ReturnsTrueForAttributesOnProperties()
    {
        // Arrange
        var source = @"
using System;
namespace TestApp
{
    public class TestAttribute : Attribute { }

    public class TestController
    {
        [TestAttribute]
        public string SomeProperty { get; set; }
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName("TestApp.TestAttribute");
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestController");
        var property = (IPropertySymbol)testClass.GetMembers("SomeProperty").First();

        // Act
        var hasAttribute = CodeAnalysisExtensions.HasAttribute(property, attribute, inherit: false);

        // Assert
        Assert.True(hasAttribute);
    }

    [Fact]
    public void HasAttribute_ReturnsTrueForAttributesOnOverridenProperties()
    {
        // Arrange
        var source = @"
using System;
namespace TestApp
{
    public class TestAttribute : Attribute { }

    public class TestControllerBase
    {
        [TestAttribute]
        public virtual string SomeProperty { get; set; }
    }

    public class TestController : TestControllerBase
    {
        public override string SomeProperty { get; set; }
    }
}";

        var compilation = TestCompilation.Create(source);
        var attribute = compilation.GetTypeByMetadataName("TestApp.TestAttribute");
        var testClass = compilation.GetTypeByMetadataName("TestApp.TestController");
        var property = (IPropertySymbol)testClass.GetMembers("SomeProperty").First();

        // Act
        var hasAttributeWithoutInherit = CodeAnalysisExtensions.HasAttribute(property, attribute, inherit: false);
        var hasAttributeWithInherit = CodeAnalysisExtensions.HasAttribute(property, attribute, inherit: true);

        // Assert
        Assert.False(hasAttributeWithoutInherit);
        Assert.True(hasAttributeWithInherit);
    }

    [Fact]
    public void IsAssignable_ReturnsFalseForDifferentTypes()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class TypeA { }

    public class TypeB { }
}";

        var compilation = TestCompilation.Create(source);
        var sourceType = compilation.GetTypeByMetadataName("TestApp.TypeA");
        var target = compilation.GetTypeByMetadataName("TestApp.TypeB");

        // Act
        var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(sourceType, target);

        // Assert
        Assert.False(isAssignableFrom);
    }

    [Fact]
    public void IsAssignable_ReturnsFalseIfTypeDoesNotImplementInterface()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class TypeA { }
}";

        var compilation = TestCompilation.Create(source);
        var sourceType = compilation.GetTypeByMetadataName("TestApp.TypeA");
        var target = compilation.GetTypeByMetadataName("System.IDisposable");

        // Act
        var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(sourceType, target);

        // Assert
        Assert.False(isAssignableFrom);
    }

    [Fact]
    public void IsAssignable_ReturnsTrueIfTypesAreExact()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public class TestType  {  }
}";

        var compilation = TestCompilation.Create(source);
        var sourceType = compilation.GetTypeByMetadataName("TestApp.TestType");
        var target = compilation.GetTypeByMetadataName("TestApp.TestType");

        // Act
        var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(sourceType, target);

        // Assert
        Assert.True(isAssignableFrom);
    }

    [Fact]
    public void IsAssignable_ReturnsTrueIfTypeImplementsInterface()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public interface ITestInterface
    {
    }

    public class TestType : ITestInterface { }
}";

        var compilation = TestCompilation.Create(source);
        var sourceType = compilation.GetTypeByMetadataName("TestApp.ITestInterface");
        var target = compilation.GetTypeByMetadataName("TestApp.TestType");

        // Act
        var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(sourceType, target);
        var isAssignableFromDerived = CodeAnalysisExtensions.IsAssignableFrom(target, sourceType);

        // Assert
        Assert.True(isAssignableFrom);
        Assert.False(isAssignableFromDerived); // Inverse shouldn't be true
    }

    [Fact]
    public void IsAssignable_ReturnsTrue_IfSourceAndDestinationAreTheSameInterface()
    {
        // Arrange
        var source = @"
namespace TestApp
{
    public interface ITestInterface { }
}";

        var compilation = TestCompilation.Create(source);
        var sourceType = compilation.GetTypeByMetadataName("TestApp.ITestInterface");
        var target = compilation.GetTypeByMetadataName("TestApp.ITestInterface");

        // Act
        var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(sourceType, target);

        // Assert
        Assert.True(isAssignableFrom);
    }

    [Fact]
    public void IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterface()
    {
        // Arrange
        var source = @"
namespace TestApp
{
public interface ITestInterface
{
}

public class ITestInterfaceA : ITestInterface
{
}

public class ITestInterfaceB : ITestInterfaceA
{
}

public class TestClass : ITestInterfaceB
{
}";
        var compilation = TestCompilation.Create(source);
        var sourceType = compilation.GetTypeByMetadataName("TestApp.ITestInterface");
        var target = compilation.GetTypeByMetadataName("TestApp.TestClass");

        // Act
        var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(sourceType, target);
        var isAssignableFromDerived = CodeAnalysisExtensions.IsAssignableFrom(target, sourceType);

        // Assert
        Assert.True(isAssignableFrom);
        Assert.False(isAssignableFromDerived); // Inverse shouldn't be true
    }
}
