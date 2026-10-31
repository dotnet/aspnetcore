// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class AsyncDisposableController : Controller, IAsyncDisposable
{
    private readonly ControllerTestDisposeAsync _testDisposeAsync;

    public AsyncDisposableController(ILogger<AsyncDisposableController> logger, ControllerTestDisposeAsync testDisposeAsync)
    {
        Logger = logger;
        _testDisposeAsync = testDisposeAsync;
    }

    public ILogger Logger { get; }

    public bool Async { get; private set; }
    public bool Throw { get; private set; }

    [HttpGet("Disposal/DisposeMode/Async({asyncMode})/Throws({throwException})")]
    public IActionResult SetDisposeMode(bool asyncMode, bool throwException)
    {
        Async = asyncMode;
        Throw = throwException;

        return Ok();
    }

    public async ValueTask DisposeAsync()
    {
        _testDisposeAsync.DisposeAsyncInvoked = true;
        if (Async)
        {
            await Task.Yield();
        }

        if (Throw)
        {
            throw new InvalidOperationException("Exception during disposal!");
        }
    }
}

public class ControllerTestDisposeAsync
{
    public bool DisposeAsyncInvoked { get; set; }
}
