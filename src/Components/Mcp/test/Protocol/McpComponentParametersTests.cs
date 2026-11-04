// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Mcp.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Mcp;

public class McpComponentParametersTests
{
    [Fact]
    public void Capture_FreezesDataAndRestoresIndependentInstances()
    {
        var original = new ParameterModel { Name = "original", Values = [17, 29] };
        var snapshot = McpComponentParameters.Capture(new Dictionary<string, object?> { ["Model"] = original, ["Optional"] = null });
        original.Name = "changed";
        original.Values.Clear();

        var first = snapshot.Restore();
        var firstModel = Assert.IsType<ParameterModel>(first["Model"]);
        firstModel.Name = "first-only";
        firstModel.Values.Add(41);
        var second = snapshot.Restore();
        var secondModel = Assert.IsType<ParameterModel>(second["Model"]);

        Assert.NotSame(original, firstModel);
        Assert.NotSame(firstModel, secondModel);
        Assert.Equal("original", secondModel.Name);
        Assert.Equal(new[] { 17, 29 }, secondModel.Values);
        Assert.Null(second["Optional"]);
    }

    [Fact]
    public void Capture_RejectsCaseInsensitiveDuplicateParameters()
    {
        Assert.Throws<ArgumentException>(() => McpComponentParameters.Capture(
            new Dictionary<string, object?> { ["Name"] = "first", ["name"] = "second" }));
    }

    [Fact]
    public void Capture_RejectsComponentAndDelegateState()
    {
        Assert.Throws<ArgumentException>(() => McpComponentParameters.Capture(
            new Dictionary<string, object?> { ["Component"] = new TestComponent() }));
        Assert.Throws<ArgumentException>(() => McpComponentParameters.Capture(
            new Dictionary<string, object?> { ["Callback"] = (Action)(() => { }) }));
    }

    [Fact]
    public void Capture_RejectsServiceProviders()
    {
        using var services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<ArgumentException>(() => McpComponentParameters.Capture(
            new Dictionary<string, object?> { ["Services"] = services }));
    }

    public sealed class ParameterModel
    {
        public string Name { get; set; } = "";
        public List<int> Values { get; set; } = [];
    }

    private sealed class TestComponent : ComponentBase;
}
