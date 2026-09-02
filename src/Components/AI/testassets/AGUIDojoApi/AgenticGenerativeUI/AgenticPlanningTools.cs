// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace AGUIDojoApi.AgenticGenerativeUI;

internal static class AgenticPlanningTools
{
    [Description("Create a plan with multiple steps.")]
    internal static Plan CreatePlan(
        [Description("List of step descriptions to create the plan.")] List<string> steps)
    {
        return new Plan
        {
            Steps =
            [
                .. steps.Select(description => new PlanStep
                {
                    Description = description,
                    Status = PlanStepStatus.Pending,
                }),
            ],
        };
    }

    [Description("Update a step in the plan with new description or status.")]
    internal static async Task<List<JsonPatchOperation>> UpdatePlanStepAsync(
        [Description("The index of the step to update.")] int index,
        [Description("The new description for the step (optional).")] string? description = null,
        [Description("The new status for the step (optional).")] PlanStepStatus? status = null)
    {
        var changes = new List<JsonPatchOperation>();

        if (description is not null)
        {
            changes.Add(new JsonPatchOperation
            {
                Op = "replace",
                Path = $"/steps/{index}/description",
                Value = description,
            });
        }

        if (status is not null)
        {
            changes.Add(new JsonPatchOperation
            {
                Op = "replace",
                Path = $"/steps/{index}/status",
                Value = status is PlanStepStatus.Pending ? "pending" : "completed",
            });
        }

        await Task.Delay(1000);

        return changes;
    }
}
