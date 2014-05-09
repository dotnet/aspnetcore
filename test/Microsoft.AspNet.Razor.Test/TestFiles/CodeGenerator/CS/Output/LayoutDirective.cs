// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class LayoutDirective
    {
        #line hidden
        public LayoutDirective()
        {
        }

        public override async Task ExecuteAsync()
        {
            Layout = "~/Foo/Bar/Baz";
        }
    }
}
