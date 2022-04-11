// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class EndpointMetadataContextTests
{
    [Fact]
    public void EndpointMetadataContext_Ctor_ThrowsArgumentNullException_WhenMethodInfoIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new EndpointMetadataContext(null, new List<object>(), null));
    }

    [Fact]
    public void EndpointMetadataContext_Ctor_ThrowsArgumentNullException_WhenMetadataIsNull()
    {
        Delegate handler = (int id) => { };
        var method = handler.GetMethodInfo();

        Assert.Throws<ArgumentNullException>(() => new EndpointMetadataContext(method, null, null));
    }

    [Fact]
    public void EndpointParameterMetadataContext_Ctor_ThrowsArgumentNullException_WhenParameterInfoIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new EndpointParameterMetadataContext(null, new List<object>(), null));
    }

    [Fact]
    public void EndpointParameterMetadataContext_Ctor_ThrowsArgumentNullException_WhenMetadataIsNull()
    {
        Delegate handler = (int id) => { };
        var parameter = handler.GetMethodInfo().GetParameters()[0];

        Assert.Throws<ArgumentNullException>(() => new EndpointParameterMetadataContext(parameter, null, null));
    }
}
