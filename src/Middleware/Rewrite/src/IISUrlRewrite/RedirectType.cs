// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal enum RedirectType
    {
        Permanent = StatusCodes.Status301MovedPermanently,
        Found = StatusCodes.Status302Found,
        SeeOther = StatusCodes.Status303SeeOther,
        Temporary = StatusCodes.Status307TemporaryRedirect
    }
}
