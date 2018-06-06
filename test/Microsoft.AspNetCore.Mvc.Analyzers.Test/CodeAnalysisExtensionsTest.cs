// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class CodeAnalysisExtensionsTest
    {
        [Fact]
        public async Task HasAttribute_ReturnsFalseIfSymbolDoesNotHaveAttribute()
        {
            // Arrange
            var compilation = await GetCompilation();
            var attribute = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttributeTest");
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
            var testClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.{nameof(HasAttribute_ReturnsTrueIfTypeHasAttribute)}");

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
            var testClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.{nameof(HasAttribute_ReturnsTrueIfBaseTypeHasAttribute)}");

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
            var @interface = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IHasAttribute_ReturnsTrueForInterfaceContractOnAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForInterfaceContractOnAttributeTest");
            var derivedClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForInterfaceContractOnAttributeDerived");

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
            var attribute = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForAttributesOnMethodsAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForAttributesOnMethodsTest");
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
            var attribute = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsTest");
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
            var attribute = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForAttributesOnPropertiesAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForAttributesOnProperties");
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
            var attribute = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForAttributesOnOverriddenPropertiesAttribute");
            var testClass = compilation.GetTypeByMetadataName($"{GetType().Namespace}.HasAttribute_ReturnsTrueForAttributesOnOverriddenProperties");
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
            var source = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsFalseForDifferentTypesA");
            var target = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsFalseForDifferentTypesB");

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
            var source = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsFalseForDifferentTypesA");
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
            var source = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsTrueIfTypesAreExact");
            var target = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsTrueIfTypesAreExact");

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
            var source = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsTrueIfTypeImplementsInterface");
            var target = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsTrueIfTypeImplementsInterfaceTest");

            // Act
            var isAssignableFrom = CodeAnalysisExtensions.IsAssignableFrom(source, target);
            var isAssignableFromDerived = CodeAnalysisExtensions.IsAssignableFrom(target, source);

            // Assert
            Assert.True(isAssignableFrom);
            Assert.False(isAssignableFromDerived); // Inverse shouldn't be true
        }

        [Fact]
        public async Task IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterface()
        {
            // Arrange
            var compilation = await GetCompilation();
            var source = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterface");
            var target = compilation.GetTypeByMetadataName($"{GetType().Namespace}.IsAssignable_ReturnsTrueIfAncestorTypeImplementsInterfaceTest");

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
            var project = DiagnosticProject.Create(GetType().Assembly, new[] { testSource.Source });

            return project.GetCompilationAsync();
        }
    }
}
