// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    internal static class ViewComponentTypes
    {
        public const string Assembly = "Microsoft.AspNetCore.Mvc.ViewFeatures";

        public static readonly Version AssemblyVersion = new Version(1, 1, 0, 0);

        public const string ViewComponentSuffix = "ViewComponent";

        public const string ViewComponentAttribute = "Microsoft.AspNetCore.Mvc.ViewComponentAttribute";

        public const string NonViewComponentAttribute = "Microsoft.AspNetCore.Mvc.NonViewComponentAttribute";

        public const string GenericTask = "System.Threading.Tasks.Task`1";

        public const string Task = "System.Threading.Tasks.Task";

        public const string IDictionary = "System.Collections.Generic.IDictionary`2";

        public const string AsyncMethodName = "InvokeAsync";

        public const string SyncMethodName = "Invoke";

        public static class ViewComponent
        {
            public const string Name = "Name";
        }
    }
}
