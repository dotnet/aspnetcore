// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.App.Analyzers.Infrastructure;

internal static class WellKnownTypeData
{
    public enum WellKnownType
    {
        Microsoft_AspNetCore_Http_HttpContext,
        Microsoft_AspNetCore_Http_HttpRequest,
        Microsoft_AspNetCore_Http_HttpResponse,
        Microsoft_AspNetCore_Http_IFormCollection,
        Microsoft_AspNetCore_Http_IFormFileCollection,
        Microsoft_AspNetCore_Http_IFormFile,
        Microsoft_AspNetCore_Http_IResult,
        System_IO_Pipelines_PipeReader,
        System_IO_Stream,
        System_Security_Claims_ClaimsPrincipal,
        System_Threading_CancellationToken,
        System_Threading_Tasks_Task,
        System_Threading_Tasks_Task_T,
        System_Threading_Tasks_ValueTask,
        System_Threading_Tasks_ValueTask_T,
    }

    public static readonly string[] WellKnownTypeNames = new[]
    {
        "Microsoft.AspNetCore.Http.HttpContext",
        "Microsoft.AspNetCore.Http.HttpRequest",
        "Microsoft.AspNetCore.Http.HttpResponse",
        "Microsoft.AspNetCore.Http.IFormCollection",
        "Microsoft.AspNetCore.Http.IFormFileCollection",
        "Microsoft.AspNetCore.Http.IFormFile",
        "Microsoft.AspNetCore.Http.IResult",
        "System.IO.Pipelines.PipeReader",
        "System.IO.Stream",
        "System.Security.Claims.ClaimsPrincipal",
        "System.Threading.CancellationToken",
        "System.Threading.Tasks.Task",
        "System.Threading.Tasks.Task`1",
        "System.Threading.Tasks.ValueTask",
        "System.Threading.Tasks.ValueTask`1",
    };
}
