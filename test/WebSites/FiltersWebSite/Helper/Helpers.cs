// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace FiltersWebSite
{
    public static class Helpers
    {
        public static ContentResult GetContentResult(object result, string message)
        {
            var actualResult = result as ContentResult;
            var content = message;

            if (actualResult != null)
            {
                content += ", " + actualResult.Content;
            }

            return new ContentResult()
            {
                Content = content,
                ContentType = "text/plain",
            };
        }
    }
}