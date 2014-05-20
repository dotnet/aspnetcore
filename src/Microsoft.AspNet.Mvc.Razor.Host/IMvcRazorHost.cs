// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Razor;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IMvcRazorHost
    {
        GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream);
    }
}
