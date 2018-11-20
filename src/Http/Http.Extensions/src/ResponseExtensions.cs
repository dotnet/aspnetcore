// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    public static class ResponseExtensions
    {
        public static void Clear(this HttpResponse response)
        {
            if (response.HasStarted)
            {
                throw new InvalidOperationException("The response cannot be cleared, it has already started sending.");
            }
            response.StatusCode = 200;
            response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = null;
            response.Headers.Clear();
            if (response.Body.CanSeek)
            {
                response.Body.SetLength(0);
            }
        }
    }
}
