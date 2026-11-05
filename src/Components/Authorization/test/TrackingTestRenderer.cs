// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Authorization;

// A renderer that records the tasks returned by calls to SetParametersAsync which do not complete synchronously.
public class TrackingTestRenderer(IServiceProvider serviceProvider) : TestRenderer(serviceProvider)
{
    private readonly ConcurrentQueue<Task> _parameterSetTasks = new();

    protected override void AddPendingTask(ComponentState componentState, Task task)
    {
        _parameterSetTasks.Enqueue(task);
        base.AddPendingTask(componentState, task);
    }

    // Runs the component render and returns the task for the parameter set it supplied.
    public Task CaptureRenderParameterSetTask(AutoRenderComponent component)
    {
        var countBefore = _parameterSetTasks.Count;
        component.TriggerRender();
        return AssertSingleParameterSetTaskAdded(countBefore);
    }

    // Runs the async trigger and returns the task for the parameter set it supplied.
    public async Task<Task> CaptureParameterSetTaskAsync(Func<Task> trigger)
    {
        var countBefore = _parameterSetTasks.Count;
        await trigger();
        return AssertSingleParameterSetTaskAdded(countBefore);
    }

    private Task AssertSingleParameterSetTaskAdded(int countBefore)
    {
        var tasksAfter = _parameterSetTasks.ToArray();
        var added = tasksAfter.Length - countBefore;

        Assert.True(
            added == 1,
            $"Expected the trigger to supply exactly one parameter set that suspends, but {added} were tracked.");

        return tasksAfter[^1];
    }
}
