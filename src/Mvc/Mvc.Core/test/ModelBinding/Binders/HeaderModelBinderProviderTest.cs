// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class HeaderModelBinderProviderTest
{
    public static TheoryData<BindingSource> NonHeaderBindingSources
    {
        get
        {
            return new TheoryData<BindingSource>()
                {
                    BindingSource.Body,
                    BindingSource.Form,
                    null,
                };
        }
    }

    [Theory]
    [MemberData(nameof(NonHeaderBindingSources))]
    public void Create_WhenBindingSourceIsNotFromHeader_ReturnsNull(BindingSource source)
    {
        // Arrange
        var provider = new HeaderModelBinderProvider();
        var testBinder = Mock.Of<IModelBinder>();
        var context = GetTestModelBinderProviderContext(typeof(string));
        context.OnCreatingBinder(modelMetadata => testBinder);
        context.BindingInfo.BindingSource = source;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(int))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(double))]
    [InlineData(typeof(CarEnumType))]
    public void Create_WhenBindingSourceIsFromHeader_ReturnsBinder_ForSimpleTypes(Type modelType)
    {
        // Arrange
        var provider = new HeaderModelBinderProvider();
        var testBinder = Mock.Of<IModelBinder>();
        var context = GetTestModelBinderProviderContext(modelType);
        context.OnCreatingBinder(modelMetadata => testBinder);
        context.BindingInfo.BindingSource = BindingSource.Header;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        var headerModelBinder = Assert.IsType<HeaderModelBinder>(result);
        Assert.Same(testBinder, headerModelBinder.InnerModelBinder);
    }

    [Theory]
    [InlineData(typeof(bool?))]
    [InlineData(typeof(int?))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(double?))]
    [InlineData(typeof(CarEnumType?))]
    public void Create_WhenBindingSourceIsFromHeader_ReturnsBinder_ForNullableSimpleTypes(Type modelType)
    {
        // Arrange
        var provider = new HeaderModelBinderProvider();
        var testBinder = Mock.Of<IModelBinder>();
        var context = GetTestModelBinderProviderContext(modelType);
        context.OnCreatingBinder(modelMetadata => testBinder);
        context.BindingInfo.BindingSource = BindingSource.Header;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        var headerModelBinder = Assert.IsType<HeaderModelBinder>(result);
        Assert.Same(testBinder, headerModelBinder.InnerModelBinder);
    }

    [Theory]
    [InlineData(typeof(string[]))]
    [InlineData(typeof(IEnumerable<bool>))]
    [InlineData(typeof(List<byte>))]
    [InlineData(typeof(Collection<short>))]
    [InlineData(typeof(float[]))]
    [InlineData(typeof(IEnumerable<decimal>))]
    [InlineData(typeof(List<double>))]
    [InlineData(typeof(ICollection<CarEnumType>))]
    public void Create_WhenBindingSourceIsFromHeader_ReturnsBinder_ForCollectionOfSimpleTypes(Type modelType)
    {
        // Arrange
        var provider = new HeaderModelBinderProvider();
        var testBinder = Mock.Of<IModelBinder>();
        var context = GetTestModelBinderProviderContext(modelType);
        context.OnCreatingBinder(modelMetadata => testBinder);
        context.BindingInfo.BindingSource = BindingSource.Header;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        var headerModelBinder = Assert.IsType<HeaderModelBinder>(result);
        Assert.Same(testBinder, headerModelBinder.InnerModelBinder);
    }

    [Theory]
    [InlineData(typeof(CustomerStruct))]
    [InlineData(typeof(IEnumerable<CustomerStruct>))]
    [InlineData(typeof(Person))]
    [InlineData(typeof(IEnumerable<Person>))]
    public void Create_WhenBindingSourceIsFromHeader_ReturnsNull_ForNonSimpleModelType(Type modelType)
    {
        // Arrange
        var provider = new HeaderModelBinderProvider();
        var testBinder = Mock.Of<IModelBinder>();
        var context = GetTestModelBinderProviderContext(modelType);
        context.OnCreatingBinder(modelMetadata => testBinder);
        context.BindingInfo.BindingSource = BindingSource.Header;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(typeof(ProductWithTypeConverter))]
    [InlineData(typeof(IEnumerable<ProductWithTypeConverter>))]
    [InlineData(typeof(CustomerStructWithTypeConverter))]
    [InlineData(typeof(IEnumerable<CustomerStructWithTypeConverter>))]
    public void Create_WhenBindingSourceIsFromHeader_ReturnsBinder_ForNonSimpleModelType_HavingTypeConverter(
        Type modelType)
    {
        // Arrange
        var provider = new HeaderModelBinderProvider();
        var testBinder = Mock.Of<IModelBinder>();
        var context = GetTestModelBinderProviderContext(modelType);
        context.OnCreatingBinder(modelMetadata => testBinder);
        context.BindingInfo.BindingSource = BindingSource.Header;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        var headerModelBinder = Assert.IsType<HeaderModelBinder>(result);
        Assert.Same(testBinder, headerModelBinder.InnerModelBinder);
    }

    [Fact]
    public void Create_WhenBindingSourceIsFromHeader_NoInnerBinderAvailable_ReturnsNull()
    {
        // Arrange
        var provider = new HeaderModelBinderProvider();
        var context = GetTestModelBinderProviderContext(typeof(string));
        context.OnCreatingBinder(modelMetadata => null);
        context.BindingInfo.BindingSource = BindingSource.Header;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    private TestModelBinderProviderContext GetTestModelBinderProviderContext(Type modelType)
    {
        var context = new TestModelBinderProviderContext(modelType);
        var options = context.Services.GetRequiredService<IOptions<MvcOptions>>().Value;
        return context;
    }

    private enum CarEnumType
    {
        Sedan,
        Coupe
    }

    private struct CustomerStruct
    {
        public string Name { get; set; }
    }

    [TypeConverter(typeof(CanConvertFromStringConverter))]
    private struct CustomerStructWithTypeConverter
    {
        public string Name { get; set; }
    }

    private class Person
    {
        public string Name { get; set; }
    }

    [TypeConverter(typeof(CanConvertFromStringConverter))]
    private class ProductWithTypeConverter
    {
        public string Name { get; set; }
    }

    private class CanConvertFromStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }
    }
}
