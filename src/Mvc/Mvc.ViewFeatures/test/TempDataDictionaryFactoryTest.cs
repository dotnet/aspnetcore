// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class TempDataDictionaryFactoryTest
{
    [Fact]
    public void Factory_CreatesTempData_ForEachHttpContext()
    {
        // Arrange
        var factory = CreateFactory();

        var context1 = new DefaultHttpContext();
        var context2 = new DefaultHttpContext();

        var tempData1 = factory.GetTempData(context1);

        // Act
        var tempData2 = factory.GetTempData(context2);

        // Assert
        Assert.NotSame(tempData1, tempData2);
    }

    [Fact]
    public void Factory_StoresTempData_InHttpContext()
    {
        // Arrange
        var factory = CreateFactory();

        var context = new DefaultHttpContext();

        var tempData1 = factory.GetTempData(context);

        // Act
        var tempData2 = factory.GetTempData(context);

        // Assert
        Assert.Same(tempData1, tempData2);
    }

    private TempDataDictionaryFactory CreateFactory()
    {
        var provider = Mock.Of<ITempDataProvider>();
        return new TempDataDictionaryFactory(provider);
    }
}
