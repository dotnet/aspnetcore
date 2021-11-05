// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public static class CommonAnnotations
{
    public static readonly object Imported = "Imported";

    public static readonly object PrimaryClass = "PrimaryClass";

    public static readonly object PrimaryMethod = "PrimaryMethod";

    public static readonly object PrimaryNamespace = "PrimaryNamespace";

    public static class DefaultTagHelperExtension
    {
        public static readonly object TagHelperField = "TagHelperField";
    }
}
