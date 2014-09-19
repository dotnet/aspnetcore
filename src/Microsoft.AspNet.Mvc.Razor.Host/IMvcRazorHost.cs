// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Razor;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IMvcRazorHost
    {
        GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream);

        /// <summary>
        /// Represent the prefix off the main entry class in the view.
        /// </summary>
        string MainClassNamePrefix { get; }

        /// <summary>
        /// Represent the namespace the main entry class in the view.
        /// </summary>
        string DefaultNamespace { get; }
    }
}
