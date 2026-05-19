// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging;

public class HttpLoggingOptionsTests
{
    [Fact]
    public void DefaultsMediaTypes()
    {
        var options = new HttpLoggingOptions();
        var defaultMediaTypes = options.MediaTypeOptions.MediaTypeStates;
        Assert.Equal(5, defaultMediaTypes.Count);

        Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("application/json"));
        Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("application/*+json"));
        Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("application/xml"));
        Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("application/*+xml"));
        Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("text/*"));
    }

    [Fact]
    public void CanAddMediaTypesString()
    {
        var options = new HttpLoggingOptions();
        options.MediaTypeOptions.AddText("test/*");

        var defaultMediaTypes = options.MediaTypeOptions.MediaTypeStates;
        Assert.Equal(6, defaultMediaTypes.Count);

        Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.MediaType.Equals("test/*"));
    }

    [Fact]
    public void CanAddMediaTypesWithCharset()
    {
        var options = new HttpLoggingOptions();
        options.MediaTypeOptions.AddText("test/*; charset=ascii");

        var defaultMediaTypes = options.MediaTypeOptions.MediaTypeStates;
        Assert.Equal(6, defaultMediaTypes.Count);

        Assert.Contains(defaultMediaTypes, w => w.MediaTypeHeaderValue.Encoding.WebName.Equals("us-ascii"));
    }

    [Fact]
    public void CanClearMediaTypes()
    {
        var options = new HttpLoggingOptions();
        options.MediaTypeOptions.Clear();
        Assert.Empty(options.MediaTypeOptions.MediaTypeStates);
    }

    [Fact]
    public void HeadersAreCaseInsensitive()
    {
        var options = new HttpLoggingOptions();
        options.RequestHeaders.Clear();
        options.RequestHeaders.Add("Test");
        options.RequestHeaders.Add("test");

        Assert.Single(options.RequestHeaders);
    }
}
