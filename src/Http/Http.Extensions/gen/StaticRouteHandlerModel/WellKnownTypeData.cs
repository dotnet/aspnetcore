// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.App.Analyzers.Infrastructure;

internal static class WellKnownTypeData
{
    public enum WellKnownType
    {
        Microsoft_AspNetCore_Http_IResult,
        System_Threading_Tasks_Task,
        System_Threading_Tasks_Task_T,
        System_Threading_Tasks_ValueTask,
        System_Threading_Tasks_ValueTask_T
    }

    public static string[] WellKnownTypeNames = new[]
    {
        "Microsoft.AspNetCore.Http.IResult",
        "System.Threading.Tasks.Task",
        "System.Threading.Tasks.Task`1",
        "System.Threading.Tasks.ValueTask",
        "System.Threading.Tasks.ValueTask`1"
    };
}
