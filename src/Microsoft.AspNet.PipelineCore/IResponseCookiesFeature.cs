// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.PipelineCore.Collections;

namespace Microsoft.AspNet.PipelineCore
{
    public interface IResponseCookiesFeature
    {
        IResponseCookies Cookies { get; }
    }
}