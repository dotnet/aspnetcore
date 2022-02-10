// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest.Task;

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
