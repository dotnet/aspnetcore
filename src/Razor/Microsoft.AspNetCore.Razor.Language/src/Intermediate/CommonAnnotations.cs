// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
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
}
