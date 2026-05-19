// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Extensions.Internal;

public class PropertyActivatorTest
{
    [Fact]
    public void Activate_InvokesValueAccessorWithExpectedValue()
    {
        // Arrange
        var instance = new TestClass();
        var typeInfo = instance.GetType().GetTypeInfo();
        var property = typeInfo.GetDeclaredProperty("IntProperty");
        var invokedWith = -1;
        var activator = new PropertyActivator<int>(
            property,
            valueAccessor: (val) =>
            {
                invokedWith = val;
                return val;
            });

        // Act
        activator.Activate(instance, 123);

        // Assert
        Assert.Equal(123, invokedWith);
    }

    [Fact]
    public void Activate_SetsPropertyValue()
    {
        // Arrange
        var instance = new TestClass();
        var typeInfo = instance.GetType().GetTypeInfo();
        var property = typeInfo.GetDeclaredProperty("IntProperty");
        var activator = new PropertyActivator<int>(property, valueAccessor: (val) => val + 1);

        // Act
        activator.Activate(instance, 123);

        // Assert
        Assert.Equal(124, instance.IntProperty);
    }

    [Fact]
    public void GetPropertiesToActivate_RestrictsActivatableProperties()
    {
        // Arrange
        var instance = new TestClass();
        var typeInfo = instance.GetType().GetTypeInfo();
        var expectedPropertyInfo = typeInfo.GetDeclaredProperty("ActivatableProperty");

        // Act
        var propertiesToActivate = PropertyActivator<int>.GetPropertiesToActivate(
            type: typeof(TestClass),
            activateAttributeType: typeof(TestActivateAttribute),
            createActivateInfo:
            (propertyInfo) => new PropertyActivator<int>(propertyInfo, valueAccessor: (val) => val + 1));

        // Assert
        Assert.Collection(
            propertiesToActivate,
            (activator) =>
            {
                Assert.Equal(expectedPropertyInfo, activator.PropertyInfo);
            });
    }

    [Fact]
    public void GetPropertiesToActivate_CanCreateCustomPropertyActivators()
    {
        // Arrange
        var instance = new TestClass();
        var typeInfo = instance.GetType().GetTypeInfo();
        var expectedPropertyInfo = typeInfo.GetDeclaredProperty("IntProperty");

        // Act
        var propertiesToActivate = PropertyActivator<int>.GetPropertiesToActivate(
            type: typeof(TestClass),
            activateAttributeType: typeof(TestActivateAttribute),
            createActivateInfo:
            (propertyInfo) => new PropertyActivator<int>(expectedPropertyInfo, valueAccessor: (val) => val + 1));

        // Assert
        Assert.Collection(
            propertiesToActivate,
            (activator) =>
            {
                Assert.Equal(expectedPropertyInfo, activator.PropertyInfo);
            });
    }

    [Fact]
    public void GetPropertiesToActivate_ExcludesNonPublic()
    {
        // Arrange
        var instance = new TestClassWithPropertyVisiblity();
        var typeInfo = instance.GetType().GetTypeInfo();
        var expectedPropertyInfo = typeInfo.GetDeclaredProperty("Public");

        // Act
        var propertiesToActivate = PropertyActivator<int>.GetPropertiesToActivate(
            typeof(TestClassWithPropertyVisiblity),
            typeof(TestActivateAttribute),
            (propertyInfo) => new PropertyActivator<int>(propertyInfo, valueAccessor: (val) => val));

        // Assert
        Assert.Single(propertiesToActivate);
        Assert.Single(propertiesToActivate, p => p.PropertyInfo == expectedPropertyInfo);
    }

    [Fact]
    public void GetPropertiesToActivate_IncludesNonPublic()
    {
        // Arrange
        var instance = new TestClassWithPropertyVisiblity();
        var typeInfo = instance.GetType().GetTypeInfo();

        // Act
        var propertiesToActivate = PropertyActivator<int>.GetPropertiesToActivate(
            typeof(TestClassWithPropertyVisiblity),
            typeof(TestActivateAttribute),
            (propertyInfo) => new PropertyActivator<int>(propertyInfo, valueAccessor: (val) => val),
            includeNonPublic: true);

        // Assert
        Assert.Equal(5, propertiesToActivate.Length);
    }

    private class TestClass
    {
        public int IntProperty { get; set; }

        [TestActivate]
        public int ActivatableProperty { get; set; }

        [TestActivate]
        public int NoSetterActivatableProperty { get; }

        [TestActivate]
        public int this[int something] // Not activatable
        {
            get
            {
                return 0;
            }
        }

        [TestActivate]
        public static int StaticActivatableProperty { get; set; }
    }

    private class TestClassWithPropertyVisiblity
    {
        [TestActivate]
        public int Public { get; set; }

        [TestActivate]
        protected int Protected { get; set; }

        [TestActivate]
        internal int Internal { get; set; }

        [TestActivate]
        protected internal int ProtectedInternal { get; set; }

        [TestActivate]
        private int Private { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    private class TestActivateAttribute : Attribute
    {
    }

    private class ActivationInfo
    {
        public string Name { get; set; }
    }
}
