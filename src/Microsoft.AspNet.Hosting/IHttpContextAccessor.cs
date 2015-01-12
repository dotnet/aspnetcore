// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting
{
    public interface IHttpContextAccessor
    {
        bool IsRootContext { get; set; }
        HttpContext Value { get; }

        HttpContext SetValue(HttpContext value);
    }
}