// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class ServicesModelBinderProviderTest
{
    public static TheoryData<BindingSource> NonServicesBindingSources
    {
        get
        {
            return new TheoryData<BindingSource>()
                {
                    BindingSource.Header,
                    BindingSource.Form,
                    null,
                };
        }
    }

    [Theory]
    [MemberData(nameof(NonServicesBindingSources))]
    public void Create_WhenBindingSourceIsNotFromServices_ReturnsNull(BindingSource source)
    {
        // Arrange
        var provider = new ServicesModelBinderProvider();

        var context = new TestModelBinderProviderContext(typeof(IPersonService));
        context.BindingInfo.BindingSource = source;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_WhenBindingSourceIsFromServices_ReturnsBinder()
    {
        // Arrange
        var provider = new ServicesModelBinderProvider();

        var context = new TestModelBinderProviderContext(typeof(IPersonService));
        context.BindingInfo.BindingSource = BindingSource.Services;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<ServicesModelBinder>(result);
    }

    [Theory]
    [MemberData(nameof(ParameterInfoData))]
    public void Create_WhenBindingSourceIsNullableFromServices_ReturnsBinder(ParameterInfo parameterInfo, bool isOptional)
    {
        // Arrange
        var provider = new ServicesModelBinderProvider();

        var context = new TestModelBinderProviderContext(parameterInfo);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        var binder = Assert.IsType<ServicesModelBinder>(result);
        Assert.Equal(isOptional, binder.IsOptional);
    }

    private class IPersonService
    {
    }

    public static TheoryData<ParameterInfo, bool> ParameterInfoData()
    {
        return new TheoryData<ParameterInfo, bool>()
        {
            { ParameterInfos.NullableParameterInfo, true },
            { ParameterInfos.DefaultValueParameterInfo, true },
            { ParameterInfos.NonNullabilityContextParameterInfo, false },
            { ParameterInfos.NonNullableParameterInfo, false },
        };
    }

    private class ParameterInfos
    {
#nullable disable
        public void TestMethod([FromServices] IPersonService param1, [FromServices] IPersonService param2 = null)
        { }
#nullable restore

#nullable enable
        public void TestMethod2([FromServices] IPersonService param1, [FromServices] IPersonService? param2)
        { }
#nullable restore

        public static ParameterInfo NonNullableParameterInfo
            = typeof(ParameterInfos)
                .GetMethod(nameof(ParameterInfos.TestMethod2))
                .GetParameters()[0];
        public static ParameterInfo NullableParameterInfo
            = typeof(ParameterInfos)
                .GetMethod(nameof(ParameterInfos.TestMethod2))
                .GetParameters()[1];

        public static ParameterInfo NonNullabilityContextParameterInfo
            = typeof(ParameterInfos)
                .GetMethod(nameof(ParameterInfos.TestMethod))
                .GetParameters()[0];
        public static ParameterInfo DefaultValueParameterInfo
            = typeof(ParameterInfos)
                .GetMethod(nameof(ParameterInfos.TestMethod))
                .GetParameters()[1];
    }
}
