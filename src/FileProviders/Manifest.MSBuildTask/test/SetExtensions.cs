// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest.Task
{
    internal static class SetExtensions
    {
        public static Entry AddRange(this Entry source, params Entry[] elements)
        {
            foreach (var element in elements)
            {
                source.Children.Add(element);
            }

            return source;
        }
    }
}
