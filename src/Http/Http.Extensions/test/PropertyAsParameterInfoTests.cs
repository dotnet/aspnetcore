// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class PropertyAsParameterInfoTests
{
    [Fact]
    public void Initialization_SetsTypeAndNameFromPropertyInfo()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo);

        // Assert
        Assert.Equal(propertyInfo.Name, parameterInfo.Name);
        Assert.Equal(propertyInfo.PropertyType, parameterInfo.ParameterType);
    }

    [Fact]
    public void Initialization_WithConstructorArgument_SetsTypeAndNameFromPropertyInfo()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.NoAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo, parameter);

        // Assert
        Assert.Equal(propertyInfo.Name, parameterInfo.Name);
        Assert.Equal(propertyInfo.PropertyType, parameterInfo.ParameterType);
    }

    [Fact]
    public void PropertyAsParameterInfoTests_ContainsPropertyCustomAttributes()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo);

        // Act & Assert
        Assert.Single(parameterInfo.GetCustomAttributes(typeof(TestAttribute)));
    }

    [Fact]
    public void PropertyAsParameterInfoTests_WithConstructorArgument_UsesParameterCustomAttributes()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.WithTestAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo, parameter);

        // Act & Assert
        Assert.Single(parameterInfo.GetCustomAttributes(typeof(TestAttribute)));
    }

    [Fact]
    public void PropertyAsParameterInfoTests_WithConstructorArgument_FallbackToPropertyCustomAttributes()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.NoAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo, parameter);

        // Act & Assert
        Assert.Single(parameterInfo.GetCustomAttributes(typeof(TestAttribute)));
    }

    [Fact]
    public void PropertyAsParameterInfoTests_ContainsPropertyCustomAttributesData()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo);

        // Act
        var attributes = parameterInfo.GetCustomAttributesData();

        // Assert
        Assert.Single(
            attributes,
            a => typeof(TestAttribute).IsAssignableFrom(a.AttributeType));
    }

    [Fact]
    public void PropertyAsParameterInfoTests_WithConstructorArgument_MergePropertyAndParameterCustomAttributesData()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.WithSampleAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo, parameter);

        // Act
        var attributes = parameterInfo.GetCustomAttributesData();

        // Assert
        Assert.Single(
            parameterInfo.GetCustomAttributesData(),
            a => typeof(TestAttribute).IsAssignableFrom(a.AttributeType));
        Assert.Single(
            parameterInfo.GetCustomAttributesData(),
            a => typeof(SampleAttribute).IsAssignableFrom(a.AttributeType));
    }

    [Fact]
    public void PropertyAsParameterInfoTests_WithConstructorArgument_MergePropertyAndParameterCustomAttributes()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.WithSampleAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo, parameter);

        // Act
        var attributes = parameterInfo.GetCustomAttributes(true);

        // Assert
        Assert.Single(
            attributes,
            a => typeof(TestAttribute).IsAssignableFrom(a.GetType()));
        Assert.Single(
            attributes,
            a => typeof(SampleAttribute).IsAssignableFrom(a.GetType()));
    }

    [Fact]
    public void PropertyAsParameterInfoTests_ContainsPropertyInheritedCustomAttributes()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(DerivedArgumentList), nameof(DerivedArgumentList.WithTestAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo);

        // Assert
        Assert.Single(parameterInfo.GetCustomAttributes(typeof(TestAttribute), true));
    }

    [Fact]
    public void PropertyAsParameterInfoTests_DoesNotHaveDefaultValueFromProperty()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo);

        // Assert
        Assert.False(parameterInfo.HasDefaultValue);
    }

    [Fact]
    public void PropertyAsParameterInfoTests_WithConstructorArgument_HasDefaultValue()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), "withDefaultValue");
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo, parameter);

        // Assert
        Assert.True(parameterInfo.HasDefaultValue);
        Assert.NotNull(parameterInfo.DefaultValue);
        Assert.IsType<int>(parameterInfo.DefaultValue);
        Assert.NotNull(parameterInfo.RawDefaultValue);
        Assert.IsType<int>(parameterInfo.RawDefaultValue);
    }

    [Fact]
    public void PropertyAsParameterInfoTests_WithConstructorArgument_DoesNotHaveDefaultValue()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.NoAttribute));
        var parameterInfo = new PropertyAsParameterInfo(propertyInfo, parameter);

        // Assert
        Assert.False(parameterInfo.HasDefaultValue);
    }

    private static PropertyInfo GetProperty(Type containerType, string propertyName)
        => containerType.GetProperty(propertyName);

    private static ParameterInfo GetParameter(string methodName, string parameterName)
    {
        var methodInfo = typeof(ArgumentList).GetMethod(methodName);
        var parameters = methodInfo.GetParameters();
        return parameters.Single(p => p.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
    }

    private class ArgumentList
    {
        public int NoAttribute { get; set; }

        [Test]
        public virtual int WithTestAttribute { get; set; }

        [Sample]
        public int WithSampleAttribute { get; set; }

        public void DefaultMethod(
            int noAttribute,
            [Test] int withTestAttribute,
            [Sample] int withSampleAttribute,
            int withDefaultValue = 10)
        { }
    }

    private class DerivedArgumentList : ArgumentList
    {
        [DerivedTest]
        public override int WithTestAttribute
        {
            get => base.WithTestAttribute;
            set => base.WithTestAttribute = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = true)]
    private class SampleAttribute : Attribute
    { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = true)]
    private class TestAttribute : Attribute
    { }

    private class DerivedTestAttribute : TestAttribute
    { }
}
