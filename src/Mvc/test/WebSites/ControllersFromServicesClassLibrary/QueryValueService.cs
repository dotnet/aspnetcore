// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ControllersFromServicesClassLibrary
{
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
}