// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ContentNegotiation;

public class TextPlainController : Controller
{
    public Task<string> ReturnTaskOfString()
    {
        return Task.FromResult<string>("ReturnTaskOfString");
    }

    public Task<object> ReturnTaskOfObject_StringValue()
    {
        return Task.FromResult<object>("ReturnTaskOfObject_StringValue");
    }

    public Task<object> ReturnTaskOfObject_ObjectValue()
    {
        return Task.FromResult(new object());
    }

    public string ReturnString()
    {
        return "ReturnString";
    }

    public object ReturnObject_StringValue()
    {
        return "ReturnObject_StringValue";
    }

    public object ReturnObject_ObjectValue()
    {
        return new object();
    }

    public string ReturnString_NullValue()
    {
        return null;
    }

    public object ReturnObject_NullValue()
    {
        return null;
    }
}
