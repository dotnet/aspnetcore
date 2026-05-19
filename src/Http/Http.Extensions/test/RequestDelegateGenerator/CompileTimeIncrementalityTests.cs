// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public class CompileTimeIncrementalityTests : RequestDelegateCreationTestBase
{
    protected override bool IsGeneratorEnabled { get; } = true;

    [Fact]
    public async Task MapAction_SameReturnType_DoesNotTriggerUpdate()
    {
        var source = @"app.MapGet(""/hello"", () => ""Hello world!"");";
        var updatedSource = @"app.MapGet(""/hello"", () => ""Bye world!"");";

        var (result, compilation) = await RunGeneratorAsync(source, updatedSource);
        var outputSteps = GetRunStepOutputs(result);

        Assert.Collection(outputSteps,
            // First source output for diagnostics is unchanged.
            step => Assert.Equal(IncrementalStepRunReason.Unchanged, step.Reason),
            // Interceptable location is different across compilations
            step => Assert.Equal(IncrementalStepRunReason.Modified, step.Reason)
        );
    }

    [Fact]
    public async Task MapAction_DifferentRoutePattern_DoesNotTriggerUpdate()
    {
        var source = @"app.MapGet(""/hello"", () => ""Hello world!"");";
        var updatedSource = @"app.MapGet(""/hello-2"", () => ""Hello world!"");";

        var (result, compilation) = await RunGeneratorAsync(source, updatedSource);
        var outputSteps = GetRunStepOutputs(result);

        Assert.Collection(outputSteps,
            // First source output for diagnostics is unchanged.
            step => Assert.Equal(IncrementalStepRunReason.Unchanged, step.Reason),
            // Interceptable location is different across compilations
            step => Assert.Equal(IncrementalStepRunReason.Modified, step.Reason)
        );
    }

    [Fact]
    public async Task MapAction_ChangeReturnType_TriggersUpdate()
    {
        var source = @"app.MapGet(""/hello"", () => ""Hello world!"");";
        var updatedSource = @"app.MapGet(""/hello"", () => Task.FromResult(""Hello world!""));";

        var (result, compilation) = await RunGeneratorAsync(source, updatedSource);
        var outputSteps = GetRunStepOutputs(result);

        Assert.Collection(outputSteps,
            // First source output for diagnostics is unchanged.
            step => Assert.Equal(IncrementalStepRunReason.Unchanged, step.Reason),
            // Second source output for generated code is changed.
            step => Assert.Equal(IncrementalStepRunReason.Modified, step.Reason)
        );
    }

    [Fact]
    public async Task MapAction_ChangeBodyParamNullability_TriggersUpdate_ForSourceOnly()
    {
        var source = $"""app.MapGet("/", ([{typeof(FromBodyAttribute)}] {typeof(Todo)} todo) => TypedResults.Ok(todo));""";
        var updatedSource = $"""
#pragma warning disable CS8622
app.MapGet("/", ([{typeof(FromBodyAttribute)}] {typeof(Todo)}? todo) => TypedResults.Ok(todo));
#pragma warning disable CS8622
""";

        var (result, compilation) = await RunGeneratorAsync(source, updatedSource);
        var outputSteps = GetRunStepOutputs(result);

        Assert.Collection(outputSteps,
            // First source output for diagnostics is unchanged.
            step => Assert.Equal(IncrementalStepRunReason.Unchanged, step.Reason),
            // Second source output for generated code is changed.
            step => Assert.Equal(IncrementalStepRunReason.Modified, step.Reason)
        );
    }

    private static IEnumerable<(object Value, IncrementalStepRunReason Reason)> GetRunStepOutputs(GeneratorRunResult? result)
        => result?.TrackedOutputSteps
            .SelectMany(step => step.Value)
            .SelectMany(value => value.Outputs);
}
