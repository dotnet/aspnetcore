// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class CodeAnalysisExtensionsTest
    {
        private static readonly string Namespace = typeof(CodeAnalysisExtensionsTest).Namespace;

        [Fact]
        public async Task GetAttributes_OnMethodWithoutAttributes()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetAttributes_OnMethodWithoutAttributesClass)}");
            var method = (IMethodSymbol)testClass.GetMembers(nameof(GetAttributes_OnMethodWithoutAttributesClass.Method)).First();

            // Act
            var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

            // Assert
            Assert.Empty(attributes);
        }

        [Fact]
        public async Task GetAttributes_OnNonOverriddenMethod_ReturnsAllAttributesOnCurrentAction()
        {
            // Arrange
            var compilation = await GetCompilation("GetAttributes_WithoutMethodOverriding");
            var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetAttributes_WithoutMethodOverriding)}");
            var method = (IMethodSymbol)testClass.GetMembers(nameof(GetAttributes_WithoutMethodOverriding.Method)).First();

            // Act
            var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

            // Assert
            Assert.Collection(
                attributes,
                attributeData => Assert.Equal(201, attributeData.ConstructorArguments[0].Value));
        }

        [Fact]
        public async Task GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentAction()
        {
            // Arrange
            var compilation = await GetCompilation("GetAttributes_WithMethodOverridding");
            var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionClass)}");
            var method = (IMethodSymbol)testClass.GetMembers(nameof(GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionClass.Method)).First();

            // Act
            var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: false);

            // Assert
            Assert.Collection(
                attributes,
                attributeData => Assert.Equal(400, attributeData.ConstructorArguments[0].Value));
        }

        [Fact]
        public async Task GetAttributesSymbolOverload_OnMethodSymbol()
        {
            // Arrange
            var compilation = await GetCompilation("GetAttributes_WithMethodOverridding");
            var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionClass)}");
            var method = (IMethodSymbol)testClass.GetMembers(nameof(GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionClass.Method)).First();

            // Act
            var attributes = CodeAnalysisExtensions.GetAttributes(symbol: method, attribute: attribute);

            // Assert
            Assert.Collection(
                attributes,
                attributeData => Assert.Equal(400, attributeData.ConstructorArguments[0].Value));
        }

        [Fact]
        public async Task GetAttributes_WithInheritTrue_ReturnsAllAttributesOnCurrentActionAndOverridingMethod()
        {
            // Arrange
            var compilation = await GetCompilation("GetAttributes_WithMethodOverridding");
            var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionClass)}");
            var method = (IMethodSymbol)testClass.GetMembers(nameof(GetAttributes_WithInheritFalse_ReturnsAllAttributesOnCurrentActionClass.Method)).First();

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
        public async Task GetAttributes_OnNewMethodOfVirtualBaseMethod()
        {
            // Arrange
            var compilation = await GetCompilation("GetAttributes_WithNewMethod");
            var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetAttributes_WithNewMethodDerived)}");
            var method = (IMethodSymbol)testClass.GetMembers(nameof(GetAttributes_WithNewMethodDerived.VirtualMethod)).First();

            // Act
            var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

            // Assert
            Assert.Collection(
                attributes,
                attributeData => Assert.Equal(400, attributeData.ConstructorArguments[0].Value));
        }

        [Fact]
        public async Task GetAttributes_OnNewMethodOfNonVirtualBaseMethod()
        {
            // Arrange
            var compilation = await GetCompilation("GetAttributes_WithNewMethod");
            var attribute = compilation.GetTypeByMetadataName(typeof(ProducesResponseTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(GetAttributes_WithNewMethodDerived)}");
            var method = (IMethodSymbol)testClass.GetMembers(nameof(GetAttributes_WithNewMethodDerived.NotVirtualMethod)).First();

            // Act
            var attributes = CodeAnalysisExtensions.GetAttributes(method, attribute, inherit: true);

            // Assert
            Assert.Collection(
                attributes,
                attributeData => Assert.Equal(401, attributeData.ConstructorArguments[0].Value));
        }

        [Fact]
        public async Task GetAttributes_OnTypeWithoutAttributes()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName(typeof(GetAttributes_OnTypeWithoutAttributesType).FullName);

            // Act
            var attributes = CodeAnalysisExtensions.GetAttributes(testClass, attribute, inherit: true);

            // Assert
            Assert.Empty(attributes);
        }

        [Fact]
        public async Task GetAttributes_OnTypeWithAttributes()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName(typeof(GetAttributes_OnTypeWithAttributes).FullName);

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
        public async Task GetAttributes_BaseTypeWithAttributes()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName(typeof(GetAttributes_BaseTypeWithAttributesDerived).FullName);

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
        public async Task GetAttributes_OnDerivedTypeWithInheritFalse()
        {
            // Arrange
            var compilation = await GetCompilation(nameof(GetAttributes_BaseTypeWithAttributes));
            var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName(typeof(GetAttributes_BaseTypeWithAttributesDerived).FullName);

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
        public async Task GetAttributesSymbolOverload_OnTypeSymbol()
        {
            // Arrange
            var compilation = await GetCompilation(nameof(GetAttributes_BaseTypeWithAttributes));
            var attribute = compilation.GetTypeByMetadataName(typeof(ApiConventionTypeAttribute).FullName);
            var testClass = compilation.GetTypeByMetadataName(typeof(GetAttributes_BaseTypeWithAttributesDerived).FullName);

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
        public async Task HasAttribute_ReturnsFalseIfSymbolDoesNotHaveAttribute()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttributeTest");
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
        public async Task HasAttribute_ReturnsTrueIfTypeHasAttribute()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(HasAttribute_ReturnsTrueIfTypeHasAttribute)}");

            // Act
            var hasAttribute = CodeAnalysisExtensions.HasAttribute(testClass, attribute, inherit: false);

            // Assert
            Assert.True(hasAttribute);
        }

        [Fact]
        public async Task HasAttribute_ReturnsTrueIfBaseTypeHasAttribute()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.{nameof(HasAttribute_ReturnsTrueIfBaseTypeHasAttribute)}");

            // Act
            var hasAttributeWithoutInherit = CodeAnalysisExtensions.HasAttribute(testClass, attribute, inherit: false);
            var hasAttributeWithInherit = CodeAnalysisExtensions.HasAttribute(testClass, attribute, inherit: true);

            // Assert
            Assert.False(hasAttributeWithoutInherit);
            Assert.True(hasAttributeWithInherit);
        }

        [Fact]
        public async Task HasAttribute_ReturnsTrueForInterfaceContractOnAttribute()
        {
            // Arrange
            var compilation = await GetCompilation();
            var @interface = compilation.GetTypeByMetadataName($"{Namespace}.IHasAttribute_ReturnsTrueForInterfaceContractOnAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForInterfaceContractOnAttributeTest");
            var derivedClass = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForInterfaceContractOnAttributeDerived");

            // Act
            var hasAttribute = CodeAnalysisExtensions.HasAttribute(testClass, @interface, inherit: true);
            var hasAttributeOnDerived = CodeAnalysisExtensions.HasAttribute(testClass, @interface, inherit: true);

            // Assert
            Assert.True(hasAttribute);
            Assert.True(hasAttributeOnDerived);
        }

        [Fact]
        public async Task HasAttribute_ReturnsTrueForAttributesOnMethods()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForAttributesOnMethodsAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForAttributesOnMethodsTest");
            var method = (IMethodSymbol)testClass.GetMembers("SomeMethod").First();

            // Act
            var hasAttribute = CodeAnalysisExtensions.HasAttribute(method, attribute, inherit: false);

            // Assert
            Assert.True(hasAttribute);
        }

        [Fact]
        public async Task HasAttribute_ReturnsTrueForAttributesOnOverriddenMethods()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsTest");
            var method = (IMethodSymbol)testClass.GetMembers("SomeMethod").First();


            // Act
            var hasAttributeWithoutInherit = CodeAnalysisExtensions.HasAttribute(method, attribute, inherit: false);
            var hasAttributeWithInherit = CodeAnalysisExtensions.HasAttribute(method, attribute, inherit: true);

            // Assert
            Assert.False(hasAttributeWithoutInherit);
            Assert.True(hasAttributeWithInherit);
        }

        [Fact]
        public async Task HasAttribute_ReturnsTrueForAttributesOnProperties()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForAttributesOnPropertiesAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForAttributesOnProperties");
            var property = (IPropertySymbol)testClass.GetMembers("SomeProperty").First();

            // Act
            var hasAttribute = CodeAnalysisExtensions.HasAttribute(property, attribute, inherit: false);

            // Assert
            Assert.True(hasAttribute);
        }

        [Fact]
        public async Task HasAttribute_ReturnsTrueForAttributesOnOverridenProperties()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForAttributesOnOverriddenPropertiesAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{Namespace}.HasAttribute_ReturnsTrueForAttributesOnOverriddenProperties");
            var property = (IPropertySymbol)testClass.GetMembers("SomeProperty").First();

            // Act
            var hasAttributeWithoutInherit = CodeAnalysisExtensions.HasAttribute(property, attribute, inherit: false);
            var hasAttributeWithInherit = CodeAnalysisExtensions.HasAttribute(property, attribute, inherit: true);

            // Assert
            Assert.False(hasAttributeWithoutInherit);
            Assert.True(hasAttributeWithInherit);
        }

        [Fact]
        public async Task IsAssignable_ReturnsFalseForDifferentTypes()
        {
            // Arrange
            var compilation = await GetCompilation();
            var source = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsFalseForDifferentTypesA");
            var target = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsFalseForDifferentTypesB");

            // Act
            var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(source, target);

            // Assert
            Assert.False(isAssignableFrom);
        }

        [Fact]
        public async Task IsAssignable_ReturnsFalseIfTypeDoesNotImplementInterface()
        {
            // Arrange
            var compilation = await GetCompilation(nameof(IsAssignable_ReturnsFalseForDifferentTypes));
            var source = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsFalseForDifferentTypesA");
            var target = compilation.GetTypeByMetadataName($"System.IDisposable");

            // Act
            var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(source, target);

            // Assert
            Assert.False(isAssignableFrom);
        }

        [Fact]
        public async Task IsAssignable_ReturnsTrueIfTypesAreExact()
        {
            // Arrange
            var compilation = await GetCompilation();
            var source = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsTrueIfTypesAreExact");
            var target = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsTrueIfTypesAreExact");

            // Act
            var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(source, target);

            // Assert
            Assert.True(isAssignableFrom);
        }

        [Fact]
        public async Task IsAssignable_ReturnsTrueIfTypeImplementsInterface()
        {
            // Arrange
            var compilation = await GetCompilation();
            var source = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsTrueIfTypeImplementsInterface");
            var target = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsTrueIfTypeImplementsInterfaceTest");

            // Act
            var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(source, target);
            var isAssignableFromDerived = CodeAnalysisExtensions.IsAssignableFrom(target, source);

            // Assert
            Assert.True(isAssignableFrom);
            Assert.False(isAssignableFromDerived); // Inverse shouldn't be true
        }

        [Fact]
        public async Task IsAssignable_ReturnsTrue_IfSourceAndDestinationAreTheSameInterface()
        {
            // Arrange
            var compilation = await GetCompilation(nameof(IsAssignable_ReturnsTrueIfTypeImplementsInterface));
            var source = compilation.GetTypeByMetadataName(typeof(IsAssignable_ReturnsTrueIfTypeImplementsInterface).FullName);
            var target = compilation.GetTypeByMetadataName(typeof(IsAssignable_ReturnsTrueIfTypeImplementsInterface).FullName);

            // Act
            var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(source, target);

            // Assert
            Assert.True(isAssignableFrom);
        }

        [Fact]
        public async Task IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterface()
        {
            // Arrange
            var compilation = await GetCompilation();
            var source = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterface");
            var target = compilation.GetTypeByMetadataName($"{Namespace}.IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterfaceTest");

            // Act
            var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(source, target);
            var isAssignableFromDerived = CodeAnalysisExtensions.IsAssignableFrom(target, source);

            // Assert
            Assert.True(isAssignableFrom);
            Assert.False(isAssignableFromDerived); // Inverse shouldn't be true
        }

        private Task<Compilation> GetCompilation([CallerMemberName] string testMethod = "")
        {
            var testSource = MvcTestSource.Read(GetType().Name, testMethod);
            var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

            return project.GetCompilationAsync();
        }
    }
}
