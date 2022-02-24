// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ContentNegotiation;

public class NoContentController : Controller
{
    public Task<string> ReturnTaskOfString_NullValue()
    {
        return Task.FromResult<string>(null);
    }

    public Task<object> ReturnTaskOfObject_NullValue()
    {
        return Task.FromResult<object>(null);
    }

    public string ReturnString_NullValue()
    {
        return null;
    }

    public object ReturnObject_NullValue()
    {
        return null;
    }

    public Task ReturnTask()
    {
        return Task.FromResult<bool>(true);
    }

    public void ReturnVoid()
    {
    }
}
