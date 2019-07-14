// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public interface IHeaderPropagationProcessor
    {
        void ProcessRequest(System.Collections.Generic.IDictionary<string, Extensions.Primitives.StringValues> requestHeaders);
    }
}