// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Extensions.DependencyModel
{
    internal class ResourceAssembly
    {
        public ResourceAssembly(string path, string locale)
        {
            Locale = locale;
            Path = path;
        }

        public string Locale { get; set; }

        public string Path { get; set; }

    }
}