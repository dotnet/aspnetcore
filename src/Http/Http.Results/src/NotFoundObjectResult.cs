// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Result
{
    internal sealed class NotFoundObjectResult : ObjectResult
    {
        public NotFoundObjectResult(object? value)
            : base(value, StatusCodes.Status404NotFound)
        {
        }
    }
}
