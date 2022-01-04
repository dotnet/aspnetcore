// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

public class LifecyclePropertyTest
{
    [Fact]
    public void GetValue_GetsPropertyValue()
    {
        // Arrange
        var propertyInfo = typeof(TestSubject).GetProperty(nameof(TestSubject.TestProperty));
        var lifecycleProperty = new LifecycleProperty(propertyInfo, "test-key");
        var subject = new TestSubject { TestProperty = "test-value" };

        // Act
        var value = lifecycleProperty.GetValue(subject);

        // Assert
        Assert.Equal("test-value", value);
    }

    [Fact]
    public void SetValue_SetsPropertyValue()
    {
        // Arrange
        var propertyInfo = typeof(TestSubject).GetProperty(nameof(TestSubject.TestProperty));
        var lifecycleProperty = new LifecycleProperty(propertyInfo, "test-key");
        var subject = new TestSubject { TestProperty = "test-value" };

        // Act
        lifecycleProperty.SetValue(subject, "new-value");

        // Assert
        Assert.Equal("new-value", subject.TestProperty);
    }

    [Fact]
    public void SetValue_SetsNullPropertyValue()
    {
        // Arrange
        var propertyInfo = typeof(TestSubject).GetProperty(nameof(TestSubject.TestProperty));
        var lifecycleProperty = new LifecycleProperty(propertyInfo, "test-key");
        var subject = new TestSubject { TestProperty = "test-value" };

        // Act
        lifecycleProperty.SetValue(subject, null);

        // Assert
        Assert.Null(subject.TestProperty);
    }

    [Fact]
    public void SetValue_NoopsIfNullIsBeingAssignedToValueType()
    {
        // Arrange
        var propertyInfo = typeof(TestSubject).GetProperty(nameof(TestSubject.ValueTypeProperty));
        var lifecycleProperty = new LifecycleProperty(propertyInfo, "test-key");
        var subject = new TestSubject { ValueTypeProperty = 42 };

        // Act
        lifecycleProperty.SetValue(subject, null);

        // Assert
        Assert.Equal(42, subject.ValueTypeProperty);
    }

    [Fact]
    public void SetValue_SetsNullValue_ForNullableProperties()
    {
        // Arrange
        var propertyInfo = typeof(TestSubject).GetProperty(nameof(TestSubject.NullableProperty));
        var lifecycleProperty = new LifecycleProperty(propertyInfo, "test-key");
        var subject = new TestSubject { NullableProperty = 42 };

        // Act
        lifecycleProperty.SetValue(subject, null);

        // Assert
        Assert.Null(subject.NullableProperty);
    }

    public class TestSubject
    {
        public string TestProperty { get; set; }

        public int ValueTypeProperty { get; set; }

        public int? NullableProperty { get; set; }
    }
}
