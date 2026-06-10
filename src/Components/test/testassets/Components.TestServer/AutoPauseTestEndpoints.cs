// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestServer;

// Endpoints for AutoPauseDeferralTests that launch download deterministically on TSC.
internal static class AutoPauseTestEndpoints
{
    public static void MapAutoPauseTestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/autopause-test/download/{token}", async (string token, HttpContext context, AutoPauseTestStreamGate gate) =>
        {
            context.Response.ContentType = "application/octet-stream";
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"autopause-{token}.bin\"";

            // Emit an initial chunk so the client observes the download has started.
            var prefix = new byte[1024];
            new Random(42).NextBytes(prefix);
            await context.Response.Body.WriteAsync(prefix, context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
            gate.MarkStarted(token);

            try
            {
                // Block here until the test releases the gate (or aborts).
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                await gate.WaitAsync(token, timeout.Token);
            }
            catch (OperationCanceledException)
            {
                gate.MarkCompleted(token);
                return;
            }

            // Emit a small trailer so the client observes a clean EOF.
            var trailer = new byte[64];
            await context.Response.Body.WriteAsync(trailer, context.RequestAborted);
            gate.MarkCompleted(token);
        });

        endpoints.MapPost("/autopause-test/release/{token}", (string token, AutoPauseTestStreamGate gate) =>
        {
            var released = gate.Release(token);
            return Results.Ok(new { released });
        });

        endpoints.MapGet("/autopause-test/started/{token}", (string token, AutoPauseTestStreamGate gate) =>
            Results.Json(new { started = gate.IsStarted(token) }));

        endpoints.MapGet("/autopause-test/completed/{token}", (string token, AutoPauseTestStreamGate gate) =>
            Results.Json(new { completed = gate.IsCompleted(token) }));

        endpoints.MapPost("/autopause-test/upload/{token}", async (string token, HttpContext context, AutoPauseTestStreamGate gate) =>
        {
            var buffer = new byte[4096];
            var totalRead = 0;

            var firstRead = await context.Request.Body.ReadAsync(buffer, context.RequestAborted);
            if (firstRead == 0)
            {
                return Results.BadRequest(new { error = "empty body" });
            }
            totalRead += firstRead;
            gate.MarkStarted(token);

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                await gate.WaitAsync(token, timeout.Token);
            }
            catch (OperationCanceledException)
            {
                gate.MarkCompleted(token);
                return Results.Ok(new { totalRead, aborted = true });
            }

            int bytesRead;
            while ((bytesRead = await context.Request.Body.ReadAsync(buffer, context.RequestAborted)) > 0)
            {
                totalRead += bytesRead;
            }
            gate.MarkCompleted(token);
            return Results.Ok(new { totalRead });
        });
    }
}
