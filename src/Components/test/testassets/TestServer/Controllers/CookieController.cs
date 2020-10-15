// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace TestServer.Controllers
{
    [Route("api/[controller]/[action]")]
    [EnableCors("AllowAll")] // Only because the test client apps runs on a different origin
    public class CookieController : Controller
    {
        const string cookieKey = "test-counter-cookie";

        public string Reset()
        {
            Response.Cookies.Delete(cookieKey);
            return "Reset completed";
        }

        public string Increment()
        {
            var counter = 0;
            if (Request.Cookies.TryGetValue(cookieKey, out var incomingValue))
            {
                counter = int.Parse(incomingValue, CultureInfo.InvariantCulture);
            }

            counter++;
            Response.Cookies.Append(cookieKey, counter.ToString(CultureInfo.InvariantCulture));

            return $"Counter value is {counter}";
        }
    }
}
