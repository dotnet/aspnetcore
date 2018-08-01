// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.HttpRepl
{
    public interface IRequestInfo
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> ContentTypesByMethod { get; }

        IReadOnlyList<string> Methods { get; }

        string GetRequestBodyForContentType(string contentType, string method);
    }
}
