// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Internal;

internal static class ValueTaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task GetAsTask(this in ValueTask<FlushResult> valueTask)
    {
        // Try to avoid the allocation from AsTask
        if (valueTask.IsCompletedSuccessfully)
        {
            // Signal consumption to the IValueTaskSource
            valueTask.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }
        else
        {
            return valueTask.AsTask();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask GetAsValueTask(this in ValueTask<FlushResult> valueTask)
    {
        // Try to avoid the allocation from AsTask
        if (valueTask.IsCompletedSuccessfully)
        {
            // Signal consumption to the IValueTaskSource
            valueTask.GetAwaiter().GetResult();
            return default;
        }
        else
        {
            return new ValueTask(valueTask.AsTask());
        }
    }
}
