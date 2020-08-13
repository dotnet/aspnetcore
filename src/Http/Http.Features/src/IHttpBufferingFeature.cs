// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Features
{
    [Obsolete("See IHttpRequestBodyFeature or IHttpResponseBodyFeature DisableBuffering", error: true)]
    public interface IHttpBufferingFeature
    {
        void DisableRequestBuffering();
        void DisableResponseBuffering();
    }
}
