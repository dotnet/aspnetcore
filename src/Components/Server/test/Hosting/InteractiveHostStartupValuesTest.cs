// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

public class InteractiveHostStartupValuesTest
{
    [Fact]
    public void GetValueRejectsNullKeyBeforeInitialization()
    {
        var startupValues = new InteractiveHostStartupValues();

        Assert.Throws<ArgumentNullException>(() => startupValues.GetValue(null!));
    }

    [Fact]
    public void GetsInitializedValues()
    {
        var startupValues = new InteractiveHostStartupValues();
        startupValues.Initialize(new Dictionary<string, string>
        {
            ["key"] = "value",
        });

        Assert.Equal("value", startupValues.GetValue("key"));
        Assert.Equal("value", startupValues.GetRequired("key"));
        Assert.Null(startupValues.GetValue("missing"));
        var exception = Assert.Throws<InvalidOperationException>(() => startupValues.GetRequired("missing"));
        Assert.Equal("Startup value 'missing' was not provided.", exception.Message);
    }
}
