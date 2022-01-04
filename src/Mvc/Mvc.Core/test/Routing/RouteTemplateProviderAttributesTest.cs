// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Routing;

public class RouteTemplateProviderAttributesTest
{
    [Theory]
    [MemberData(nameof(RouteTemplateProvidersTestData))]
    public void Order_Defaults_ToNull(IRouteTemplateProvider routeTemplateProvider)
    {
        // Act & Assert
        Assert.Null(routeTemplateProvider.Order);
    }

    public static TheoryData<IRouteTemplateProvider> RouteTemplateProvidersTestData
    {
        get
        {
            var data = new TheoryData<IRouteTemplateProvider>();
            data.Add(new HttpGetAttribute());
            data.Add(new HttpPostAttribute());
            data.Add(new HttpPutAttribute());
            data.Add(new HttpPatchAttribute());
            data.Add(new HttpDeleteAttribute());
            data.Add(new HttpHeadAttribute());
            data.Add(new HttpOptionsAttribute());
            data.Add(new RouteAttribute(""));

            return data;
        }
    }
}
