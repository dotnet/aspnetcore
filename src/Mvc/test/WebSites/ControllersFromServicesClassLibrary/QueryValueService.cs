// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace ControllersFromServicesClassLibrary;

public class QueryValueService
{
    private readonly HttpContext _context;

    public QueryValueService(IHttpContextAccessor httpContextAccessor)
    {
        _context = httpContextAccessor.HttpContext;
    }

    public string GetValue()
    {
        return _context.Request.Query["value"];
    }
}
