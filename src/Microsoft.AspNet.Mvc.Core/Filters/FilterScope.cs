// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    public static class FilterScope
    {
        public static readonly int Action = 100;
        public static readonly int Controller = 200;
        public static readonly int Global = 300;
    }
}
