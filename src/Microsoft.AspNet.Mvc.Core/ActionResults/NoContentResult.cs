// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.WebUtilities;

namespace Microsoft.AspNet.Mvc
{
    public class NoContentResult : HttpStatusCodeResult
    {
        public NoContentResult()
            : base(StatusCodes.Status204NoContent)
        {
        }
    }
}
