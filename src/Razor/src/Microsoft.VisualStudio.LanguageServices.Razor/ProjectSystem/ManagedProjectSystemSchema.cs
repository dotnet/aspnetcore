// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Well-Known Schema and property names defined by the ManagedProjectSystem
    internal static class ManagedProjectSystemSchema
    {
        public static class ResolvedCompilationReference
        {
            public static readonly string SchemaName = "ResolvedCompilationReference";

            public static readonly string ItemName = "ResolvedCompilationReference";
        }

        public static class ContentItem
        {
            public static readonly string SchemaName = "Content";

            public static readonly string ItemName = "Content";
        }

        public static class NoneItem
        {
            public static readonly string SchemaName = "None";

            public static readonly string ItemName = "None";
        }

        public static class ItemReference
        {
            public static readonly string FullPathPropertyName = "FullPath";

            public static readonly string LinkPropertyName = "Link";
        }
    }
}
