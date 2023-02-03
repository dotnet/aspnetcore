// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public class RequestDelegateGeneratorIncrementalityTests : RequestDelegateGeneratorTestBase
{
    [Fact]
    public void MapAction_SameReturnType_DoesNotTriggerUpdate()
    {
        var source = @"app.MapGet(""/hello"", () => ""Hello world!"");";
        var updatedSource = @"app.MapGet(""/hello"", () => ""Bye world!"");";

        var (result, compilation) = RunGenerator(source, updatedSource);
        var outputSteps = GetRunStepOutputs(result);

        Assert.All(outputSteps, (value) => Assert.Equal(IncrementalStepRunReason.Cached, value.Reason));
    }

    [Fact]
    public void MapAction_DifferentRoutePattern_DoesNotTriggerUpdate()
    {
        var source = @"app.MapGet(""/hello"", () => ""Hello world!"");";
        var updatedSource = @"app.MapGet(""/hello-2"", () => ""Hello world!"");";

        var (result, compilation) = RunGenerator(source, updatedSource);
        var outputSteps = GetRunStepOutputs(result);

        Assert.All(outputSteps, (value) => Assert.Equal(IncrementalStepRunReason.Cached, value.Reason));
    }

    [Fact]
    public void MapAction_ChangeReturnType_TriggersUpdate()
    {
        var source = @"app.MapGet(""/hello"", () => ""Hello world!"");";
        var updatedSource = @"app.MapGet(""/hello"", () => Task.FromResult(""Hello world!""));";

        var (result, compilation) = RunGenerator(source, updatedSource);
        var outputSteps = GetRunStepOutputs(result);

        Assert.All(outputSteps, (value) => Assert.Equal(IncrementalStepRunReason.New, value.Reason));
    }

    private static IEnumerable<(object Value, IncrementalStepRunReason Reason)> GetRunStepOutputs(GeneratorRunResult result) => result.TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(value => value.Outputs);
}
