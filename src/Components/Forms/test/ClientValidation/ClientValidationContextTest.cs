// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Components.Forms;

public class ClientValidationContextTest
{
    [Fact]
    public void Constructor_SetsAttributes()
    {
        var attributes = new Dictionary<string, string>();

        var context = new ClientValidationContext(attributes);

        context.MergeAttribute("data-val", "true");
        Assert.Equal("true", attributes["data-val"]);
    }

    [Fact]
    public void Constructor_ThrowsOnNullAttributes()
    {
        Assert.Throws<ArgumentNullException>("attributes",
            () => new ClientValidationContext(null!));
    }

    [Fact]
    public void MergeAttribute_AddsNewKey()
    {
        var attributes = new Dictionary<string, string>();
        var context = new ClientValidationContext(attributes);

        var result = context.MergeAttribute("data-val", "true");

        Assert.True(result);
        Assert.Equal("true", attributes["data-val"]);
    }

    [Fact]
    public void MergeAttribute_DoesNotOverwriteExistingKey()
    {
        var attributes = new Dictionary<string, string> { ["data-val-required"] = "Original message" };
        var context = new ClientValidationContext(attributes);

        var result = context.MergeAttribute("data-val-required", "New message");

        Assert.False(result);
        Assert.Equal("Original message", attributes["data-val-required"]);
    }

    [Fact]
    public void MergeAttribute_AddsMultipleKeys()
    {
        var attributes = new Dictionary<string, string>();
        var context = new ClientValidationContext(attributes);

        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-required", "Name is required.");
        context.MergeAttribute("data-val-length", "Must be 2-100 chars.");
        context.MergeAttribute("data-val-length-min", "2");
        context.MergeAttribute("data-val-length-max", "100");

        Assert.Equal(5, attributes.Count);
        Assert.Equal("true", attributes["data-val"]);
        Assert.Equal("Name is required.", attributes["data-val-required"]);
        Assert.Equal("2", attributes["data-val-length-min"]);
        Assert.Equal("100", attributes["data-val-length-max"]);
    }

    [Fact]
    public void MergeAttribute_ThrowsOnNullKey()
    {
        var context = new ClientValidationContext(new Dictionary<string, string>());

        Assert.Throws<ArgumentNullException>("key", () => context.MergeAttribute(null!, "value"));
    }

    [Fact]
    public void MergeAttribute_ThrowsOnNullValue()
    {
        var context = new ClientValidationContext(new Dictionary<string, string>());

        Assert.Throws<ArgumentNullException>("value", () => context.MergeAttribute("key", null!));
    }

    [Fact]
    public void ContextIsReusableAcrossMultipleAdapters()
    {
        var attributes = new Dictionary<string, string>();
        var context = new ClientValidationContext(attributes);

        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-required", "Name is required.");

        context.MergeAttribute("data-val-length", "Must be 2-100 chars.");
        context.MergeAttribute("data-val-length-min", "2");
        context.MergeAttribute("data-val-length-max", "100");

        Assert.Equal(5, attributes.Count);
    }
}
